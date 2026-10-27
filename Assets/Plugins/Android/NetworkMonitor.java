package com.hbp.quest;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.ConnectivityManager;
import android.net.LinkAddress;
import android.net.LinkProperties;
import android.net.Network;
import android.net.NetworkCapabilities;
import android.net.NetworkRequest;
import android.net.nsd.NsdManager;
import android.net.nsd.NsdServiceInfo;
import android.os.Build;
import org.json.JSONArray;
import org.json.JSONObject;
import java.net.NetworkInterface;
import java.util.HashMap;
import java.util.Map;

/** Owns Network requests and NSD registrations. Callbacks only signal Unity to read a snapshot. */
public final class NetworkMonitor implements AutoCloseable {
    public interface Listener { void changedNetwork(); }
    private final Context context;
    private final Listener listener;
    private final ConnectivityManager manager;
    private final NsdManager nsd;
    private final Map<Long, Link> links = new HashMap<>();
    private ConnectivityManager.NetworkCallback usb;
    private final ConnectivityManager.NetworkCallback wifi;
    private final BroadcastReceiver cable;
    private boolean closed, foreground, declined, cableConnected;
    private String usbStatus = "USB waiting for connection";
    private long generation;

    private static final class Link {
        final Network network;
        final boolean usb;
        final long generation;
        final LinkProperties properties;
        NsdManager.RegistrationListener advertisement;
        long nextAdvertisementAttempt;
        Link(Network n, boolean u, long g, LinkProperties p) { network=n; usb=u; generation=g; properties=p; }
    }

    public NetworkMonitor(Context context, Listener listener) {
        this.context=context.getApplicationContext(); this.listener=listener;
        manager=(ConnectivityManager)context.getSystemService(Context.CONNECTIVITY_SERVICE);
        nsd=(NsdManager)context.getSystemService(Context.NSD_SERVICE);
        wifi=new ConnectivityManager.NetworkCallback() {
            @Override public void onLinkPropertiesChanged(Network n, LinkProperties p) { update(n, p, false); }
            @Override public void onLost(Network n) { lost(n); }
        };
        manager.registerNetworkCallback(new NetworkRequest.Builder().addTransportType(NetworkCapabilities.TRANSPORT_WIFI)
            .removeCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET).build(), wifi);
        cable=new BroadcastReceiver() {
            @Override public void onReceive(Context c, Intent intent) {
                synchronized(NetworkMonitor.this) {
                    boolean connected=intent.getBooleanExtra("connected", false);
                    boolean attached=connected && !cableConnected;
                    cableConnected=connected;
                    if (!connected) {
                        declined=false;
                        if (usb!=null) {
                            try { manager.unregisterNetworkCallback(usb); } catch(IllegalArgumentException ignored) { }
                            usb=null;
                        }
                        for(Link link:new java.util.ArrayList<>(links.values())) if(link.usb) lost(link.network);
                    }
                    if (attached && foreground && !declined) requestUsb();
                }
            }
        };
        IntentFilter filter=new IntentFilter("android.hardware.usb.action.USB_STATE");
        if (Build.VERSION.SDK_INT>=33) this.context.registerReceiver(cable, filter, Context.RECEIVER_NOT_EXPORTED);
        else this.context.registerReceiver(cable, filter);
    }

    private synchronized void update(Network network, LinkProperties properties, boolean isUsb) {
        if (closed) return;
        long key=network.getNetworkHandle();
        Link previous=links.get(key);
        if (previous!=null && java.util.Objects.equals(previous.properties.getInterfaceName(), properties.getInterfaceName())
            && previous.properties.getLinkAddresses().equals(properties.getLinkAddresses())) return;
        if (previous!=null) unadvertise(previous);
        links.put(key, new Link(network, isUsb, ++generation, properties));
        if (isUsb) usbStatus="USB network available";
        listener.changedNetwork();
    }

    private synchronized void lost(Network network) {
        Link link=links.remove(network.getNetworkHandle());
        if (link!=null) { unadvertise(link); if(link.usb) usbStatus="USB disconnected"; }
        if (!closed) listener.changedNetwork();
    }

    public synchronized void setForeground(boolean value) {
        foreground=value;
        if (value && !declined) requestUsb();
    }

    public synchronized void retryUsb() {
        declined=false;
        if (foreground) requestUsb();
    }

    private synchronized void requestUsb() {
        if (closed || !foreground || !cableConnected || declined || usb!=null) return;
        if (Build.VERSION.SDK_INT<33) { usbStatus="USB networking requires a newer Horizon OS. Wi-Fi remains available."; listener.changedNetwork(); return; }
        usbStatus="Waiting for USB system approval";
        ConnectivityManager.NetworkCallback callback=new ConnectivityManager.NetworkCallback() {
            @Override public void onLinkPropertiesChanged(Network n, LinkProperties p) { synchronized(NetworkMonitor.this) { if(usb==this && !closed) update(n,p,true); } }
            @Override public void onLost(Network n) { synchronized(NetworkMonitor.this) { if(usb==this && !closed) lost(n); } }
            @Override public void onUnavailable() {
                synchronized(NetworkMonitor.this) {
                    if (closed || usb!=this) return;
                    // Android has already released/unregistered this request.
                    usb=null; declined=true;
                    usbStatus="USB not approved or unavailable. Select Retry USB.";
                    listener.changedNetwork();
                }
            }
        };
        usb=callback;
        try {
            manager.requestNetwork(new NetworkRequest.Builder().addTransportType(NetworkCapabilities.TRANSPORT_USB)
                .removeCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
                .removeCapability(NetworkCapabilities.NET_CAPABILITY_TRUSTED).build(), callback, 30000);
        } catch (RuntimeException failure) {
            usb=null; declined=true; usbStatus="USB request unavailable: "+failure.getClass().getSimpleName();
        }
        listener.changedNetwork();
    }

    public synchronized String snapshot() {
        JSONObject result=new JSONObject(); JSONArray networks=new JSONArray();
        try {
            result.put("usbStatus", usbStatus);
            result.put("canRetry", declined && foreground);
            for (Link link:links.values()) {
                String name=link.properties.getInterfaceName();
                NetworkInterface iface=name==null ? null : NetworkInterface.getByName(name);
                if(iface==null) continue;
                JSONArray addresses=new JSONArray();
                for(LinkAddress address:link.properties.getLinkAddresses()) {
                    if(address.getAddress().isLoopbackAddress()) continue;
                    String value=address.getAddress().getHostAddress();
                    int zone=value.indexOf('%');
                    addresses.put(zone<0 ? value : value.substring(0,zone));
                }
                JSONObject item=new JSONObject();
                item.put("handle",link.network.getNetworkHandle()); item.put("generation",link.generation);
                item.put("index",iface.getIndex()); item.put("usb",link.usb); item.put("addresses",addresses);
                networks.put(item);
            }
            result.put("networks", networks);
        } catch (Exception failure) { android.util.Log.w("HiBoP", "Network snapshot unavailable", failure); }
        return result.toString();
    }

    public synchronized void advertise(long handle, String pin, String name) {
        Link link=links.get(handle);
        if(closed || link==null || link.advertisement!=null || android.os.SystemClock.elapsedRealtime()<link.nextAdvertisementAttempt) return;
        NsdServiceInfo info=new NsdServiceInfo();
        info.setServiceType("_hibop-quest._tcp.");
        info.setServiceName("HiBoP-"+pin.substring(0,12).replace('/','_').replace('+','_')+(link.usb ? "-usb" : "-lan"));
        info.setPort(45871);
        info.setAttribute("v","1"); info.setAttribute("id",pin); info.setAttribute("name",name); info.setAttribute("path",link.usb ? "usb" : "lan");
        if(Build.VERSION.SDK_INT>=33) info.setNetwork(link.network);
        NsdManager.RegistrationListener registration=new NsdManager.RegistrationListener() {
            @Override public void onServiceRegistered(NsdServiceInfo service) {
                synchronized(NetworkMonitor.this) {
                    if(closed || links.get(handle)!=link || link.advertisement!=this) {
                        try { nsd.unregisterService(this); } catch(IllegalArgumentException ignored) { }
                    }
                }
            }
            @Override public void onServiceUnregistered(NsdServiceInfo service) { }
            @Override public void onRegistrationFailed(NsdServiceInfo service,int code) { synchronized(NetworkMonitor.this) { if(link.advertisement==this) { link.advertisement=null; link.nextAdvertisementAttempt=android.os.SystemClock.elapsedRealtime()+5000; } } }
            @Override public void onUnregistrationFailed(NsdServiceInfo service,int code) { android.util.Log.w("HiBoP", "NSD unregister failed: "+code); }
        };
        link.advertisement=registration;
        try { nsd.registerService(info,NsdManager.PROTOCOL_DNS_SD,registration); }
        catch(RuntimeException failure) { link.advertisement=null; link.nextAdvertisementAttempt=android.os.SystemClock.elapsedRealtime()+5000; android.util.Log.w("HiBoP","NSD unavailable",failure); }
    }

    public synchronized void stopAdvertising(long handle) { Link link=links.get(handle); if(link!=null) unadvertise(link); }
    private void unadvertise(Link link) {
        if(link.advertisement==null) return;
        NsdManager.RegistrationListener registration=link.advertisement; link.advertisement=null;
        try { nsd.unregisterService(registration); } catch(IllegalArgumentException ignored) { }
    }
    public synchronized void stopAdvertising() { for(Link link:links.values()) unadvertise(link); }

    @Override public synchronized void close() {
        if(closed) return; closed=true;
        for(Link link:links.values()) unadvertise(link);
        links.clear();
        try { manager.unregisterNetworkCallback(wifi); } catch(IllegalArgumentException ignored) { }
        if(usb!=null) { try { manager.unregisterNetworkCallback(usb); } catch(IllegalArgumentException ignored) { } usb=null; }
        context.unregisterReceiver(cable);
    }
}
