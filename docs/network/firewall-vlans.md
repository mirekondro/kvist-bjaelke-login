# Firewall and communication between VLANs

The school's network firewall already allows traffic **from VLAN 570 (clients) to VLAN 571 (servers) on TCP ports 8000–8100**. We did not configure the firewall ourselves, but here is why the setup is built this way.

## Why are VLAN 570 and VLAN 571 separate networks?

A VLAN (Virtual LAN) splits one physical switch network into several separate logical networks. VLAN 570 holds the employees' computers and VLAN 571 holds the servers. They are separated because they have different security needs:

- **Different trust levels:** client computers are used for e-mail, browsing and USB sticks, so they are the most likely devices to be infected. Servers hold the company's data and the login platform's user database. If an infected client were on the same network as the servers, it could reach every port on every server directly.
- **Less broadcast traffic:** each VLAN is its own broadcast domain, so broadcasts from clients (ARP, DHCP etc.) do not reach the servers.
- **Control:** because the traffic has to leave the client network to reach the servers, we have a point (the firewall) where we can decide what is allowed.

## Why does communication between VLANs require routing?

Each VLAN is its own IP network: VLAN 570 is `10.5.70.0/24` and VLAN 571 is `10.5.71.0/24`. A switch works on layer 2 (MAC addresses) and only forwards traffic **inside** the same VLAN.

When the client `10.5.70.x` wants to reach the server `10.5.71.x`, it sees that the destination is on a **different network**, so it sends the packet to its **default gateway** (`10.5.70.1`). The gateway is a layer-3 device (router, layer-3 switch or firewall) with an interface in both VLANs. It looks at the destination IP address and forwards the packet into VLAN 571. Without a router there is no path between the two networks.

```
Client 10.5.70.x ──> gateway 10.5.70.1 ──[router/firewall]── 10.5.71.1 ──> Server 10.5.71.x
     VLAN 570                                                                    VLAN 571
```

## The role of the firewall

Since all traffic between the VLANs must pass through the router/firewall, the firewall can decide which traffic is allowed:

- It checks each connection against rules: **source** (e.g. VLAN 570), **destination** (e.g. VLAN 571), **protocol** (TCP/UDP) and **port**.
- Everything that is not explicitly allowed is blocked (the "default deny" principle).
- It is **stateful**: when a client opens a connection to the server, the reply traffic is automatically allowed back, but the server cannot start new connections to the clients.
- It can log blocked attempts, which helps detect attacks.

The firewall turns the VLANs from just separated networks into networks with controlled access between them.

## Why allow only specific ports?

Allowing only TCP 8000–8100 instead of all traffic follows the **principle of least privilege**: clients get exactly the access they need and nothing more.

- **Smaller attack surface:** a server often runs other services (SSH on port 22, databases, management interfaces). If only ports 8000–8100 are open from the client VLAN, an attacker on a client machine cannot reach SSH or the database, even if those services have a weak password or a vulnerability.
- **Limits malware spreading:** worms and ransomware often spread through SMB (445), RDP (3389) or SSH (22). Those ports are blocked between the VLANs.
- **Clear overview:** it is easy to see and audit which services are reachable from where.
- **Side effect for testing:** because only TCP 8000–8100 is allowed, `ping` (ICMP) from the client to the server may be blocked even though the web application works. We therefore test the connection with `curl` or `nc -zv <server-ip> 8080` instead of relying on `ping`.

## Our chosen port: 8080

We run the login platform on **TCP 8080**:

- it is inside the allowed range 8000–8100,
- it is the common "alternative HTTP" port, so it is easy to recognise,
- the official .NET container images listen on port 8080 by default, so the container needs no extra port configuration (`docker-compose` maps `8080:8080`).
