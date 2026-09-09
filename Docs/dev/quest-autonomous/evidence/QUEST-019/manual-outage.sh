#!/system/bin/sh
set -e
trap 'svc wifi enable' EXIT HUP INT TERM
sleep 2
echo BEFORE
date -u +%Y-%m-%dT%H:%M:%SZ
cat /proc/uptime
pidof fr.crnl.hibop.quest
svc wifi disable
sleep 1
cmd wifi status
cmd wifi status | head -n 1 | grep -q 'Wifi is disabled'
echo DISABLED
cat /proc/uptime
sleep 60
echo RESTORING
cat /proc/uptime
svc wifi enable
sleep 10
cmd wifi status
pidof fr.crnl.hibop.quest
echo COMPLETE
