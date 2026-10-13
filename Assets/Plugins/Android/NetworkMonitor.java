package com.hbp.quest;

import android.content.Context;
import android.net.ConnectivityManager;
import android.net.LinkProperties;
import android.net.Network;

/** Signals only; Unity/model operations must never run on Android's connectivity thread. */
public final class NetworkMonitor implements AutoCloseable {
    public interface Listener { void changedNetwork(); }
    private final ConnectivityManager manager;
    private final ConnectivityManager.NetworkCallback callback;
    public NetworkMonitor(Context context, final Listener listener) {
        manager = (ConnectivityManager)context.getSystemService(Context.CONNECTIVITY_SERVICE);
        callback = new ConnectivityManager.NetworkCallback() {
            @Override public void onLinkPropertiesChanged(Network network, LinkProperties properties) { listener.changedNetwork(); }
            @Override public void onLost(Network network) { listener.changedNetwork(); }
        };
        manager.registerDefaultNetworkCallback(callback);
    }
    @Override public void close() { manager.unregisterNetworkCallback(callback); }
}
