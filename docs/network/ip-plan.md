# IP addressing plan – Kvist & Bjælke ApS

## Design principles

- **Private range:** the whole company uses `10.5.0.0/16` (part of the private RFC 1918 range `10.0.0.0/8`). This matches the two networks we were given, VLAN 570 = `10.5.70.0/24` and VLAN 571 = `10.5.71.0/24`.
- **Easy-to-read numbering:** VLAN **57X** always uses the network **10.5.7X.0**. When you see an IP address you can immediately tell which VLAN it belongs to (for example `10.5.73.12` is a printer on VLAN 573). Inside a private range we have plenty of addresses, so readability matters more than saving every address.
- **Right-sized subnets:** each network gets a prefix (`/24`, `/26`, `/27`, `/28`) based on how many devices it has today, with room to grow.
- **Gateway is always `.1`:** the first usable address of every network is the default gateway (a VLAN sub-interface on the firewall/router). Staff can easily remember it and it is the same on every VLAN.
- **Segmentation by device type and trust level:** devices that need different security rules are put in different VLANs, so the firewall can control traffic between them.

## Networks

| VLAN | Name | Network (CIDR) | Subnet mask | Default gateway | Usable hosts | Devices today | Addressing |
|---|---|---|---|---|---|---|---|
| **570** | Clients (employees) | `10.5.70.0/24` | 255.255.255.0 | 10.5.70.1 | 254 | 15 desktops + 10 laptops (also via staff Wi-Fi) | DHCP |
| **571** | Servers | `10.5.71.0/24` | 255.255.255.0 | 10.5.71.1 | 254 | 3 servers (incl. the login platform server) | Static |
| 572 | Voice (IP phones) | `10.5.72.0/26` | 255.255.255.192 | 10.5.72.1 | 62 | 12 IP phones | DHCP |
| 573 | Printers | `10.5.73.0/28` | 255.255.255.240 | 10.5.73.1 | 14 | 3 network printers | Static / DHCP reservation |
| 574 | Surveillance (CCTV) | `10.5.74.0/27` | 255.255.255.224 | 10.5.74.1 | 30 | 6 IP cameras | Static |
| 575 | Guest Wi-Fi | `10.5.75.0/24` | 255.255.255.0 | 10.5.75.1 | 254 | Variable visitor devices | DHCP (short lease) |
| 579 | Network management | `10.5.79.0/28` | 255.255.255.240 | 10.5.79.1 | 14 | Firewall, switches, 4 access points | Static |

VLAN 570 and 571 were given in the assignment. VLAN 572–575 and 579 are our own proposal.

### What goes in each network and why

- **VLAN 570 – Clients:** employee desktops and laptops. The staff Wi-Fi SSID (`KB-Staff`) is mapped to VLAN 570 as well, so a laptop gets the same access whether it is cabled or wireless. This is the only VLAN that is allowed to reach the login platform (TCP 8000–8100 to VLAN 571).
- **VLAN 571 – Servers:** internal servers, including the Ubuntu Server that runs the login platform in Docker. Servers hold the company's data, so they are kept away from user devices and only reached through the firewall on specific ports.
- **VLAN 572 – Voice:** IP phones get their own VLAN so call traffic can be prioritised (QoS) and so a problem on the client network (for example a broadcast storm or malware) does not affect telephony.
- **VLAN 573 – Printers:** printers are often poorly patched. In their own VLAN, clients can be allowed to print (e.g. TCP 9100/IPP 631) while the printers cannot start connections to clients or servers.
- **VLAN 574 – Surveillance:** cameras are IoT devices with a weak security record and they send a constant video stream. They are isolated and may only talk to the video recording system (NVR), not to the internet or other VLANs.
- **VLAN 575 – Guest Wi-Fi:** visitor devices are untrusted. The guest SSID (`KB-Guest`) gives **internet access only**: no access to any internal VLAN, and client isolation is turned on in the access points so guests cannot see each other.
- **VLAN 579 – Management:** the management interfaces of switches, access points and the firewall. Only IT admins should reach these, so a normal user (or an attacker on the client VLAN) cannot log in to the network equipment.

### Should everything be on one network?

No. If servers, employee PCs, printers, phones, cameras and guests were all on one flat network:

- a guest could reach the servers directly,
- a hacked camera or printer could be used to attack employee PCs and servers,
- broadcast traffic from every device would reach every other device,
- we could not write firewall rules per device type, because all traffic would stay inside one network and never pass the firewall.

Segmentation limits what an attacker can reach (smaller "blast radius") and makes the network easier to manage and troubleshoot.

## Static IP vs. DHCP

| Type | Used for | Why |
|---|---|---|
| **Static** | Servers, network equipment, cameras | Other systems and firewall rules point to these addresses. If a server's IP changed, users could no longer reach the login platform. |
| **DHCP reservation** | Printers | The printer always gets the same IP, but it is managed centrally on the DHCP server instead of on each device. |
| **DHCP (dynamic)** | Clients, IP phones, guests | Many devices that come and go. DHCP avoids manual work and address conflicts. Guests get a short lease (e.g. 2 hours) so addresses are freed quickly. |

**Where DHCP runs:** the firewall/router acts as the DHCP server for every VLAN (one scope per VLAN). If the company later moves DHCP to a server on VLAN 571, the firewall forwards DHCP requests from the other VLANs using a DHCP relay (`ip helper-address`).

**Address ranges inside each network** (example for the /24 networks):

| Range | Purpose |
|---|---|
| `.1` | Default gateway |
| `.2 – .49` | Static addresses (servers, network equipment) |
| `.50 – .99` | DHCP reservations |
| `.100 – .250` | DHCP pool |
| `.251 – .254` | Reserved |

---

## Subnetting

The number of usable host addresses in a subnet is:

```
usable hosts = 2^(32 − prefix) − 2
```

We subtract 2 because the first address is the **network address** and the last address is the **broadcast address**. Neither can be given to a device.

### Example 1 – VLAN 572 Voice: `10.5.72.0/26`

| | |
|---|---|
| Network address | `10.5.72.0` |
| Subnet mask | `255.255.255.192` (/26) |
| Host bits | 32 − 26 = 6 → 2⁶ = 64 addresses |
| Usable hosts | 64 − 2 = **62** |
| First / last usable | `10.5.72.1` (gateway) – `10.5.72.62` |
| Broadcast | `10.5.72.63` |

**Why this size:** there are 12 IP phones today. A /28 (14 hosts) would almost be full already, and a /27 (30 hosts) only works if the company stays small. Normally every employee gets a phone, and the company has 25 employee computers today. A /26 gives room for about 5 times today's number of phones, so we do not need to renumber the network when the company grows.

### Example 2 – VLAN 573 Printers: `10.5.73.0/28`

| | |
|---|---|
| Network address | `10.5.73.0` |
| Subnet mask | `255.255.255.240` (/28) |
| Host bits | 32 − 28 = 4 → 2⁴ = 16 addresses |
| Usable hosts | 16 − 2 = **14** |
| First / last usable | `10.5.73.1` (gateway) – `10.5.73.14` |
| Broadcast | `10.5.73.15` |

**Why this size:** 3 printers + 1 gateway = 4 addresses in use. A /29 (6 hosts) would only leave 2 free addresses. A /28 leaves 10 free addresses for new printers or scanners, which is plenty, because printers are added slowly.

### Example 3 – VLAN 574 Surveillance: `10.5.74.0/27`

| | |
|---|---|
| Network address | `10.5.74.0` |
| Subnet mask | `255.255.255.224` (/27) |
| Host bits | 32 − 27 = 5 → 2⁵ = 32 addresses |
| Usable hosts | 32 − 2 = **30** |
| First / last usable | `10.5.74.1` (gateway) – `10.5.74.30` |
| Broadcast | `10.5.74.31` |

**Why this size:** 6 cameras today. Companies often add more cameras (new entrances, parking, storage), so a /27 allows the number of cameras to grow 4 times.

### Why VLAN 570 and 571 are /24

- **570 Clients (/24, 254 hosts):** 25 computers today, plus employee phones and any new hires on the staff Wi-Fi. A /24 is the standard size for a client network and has plenty of room for growth.
- **571 Servers (/24, 254 hosts):** only 3 servers today, so a /24 is larger than needed, but the size was given in the assignment. It also leaves room for more servers and virtual machines later.
- **575 Guests (/24):** we cannot predict how many visitors will connect (meetings, events), so the guest network gets a large range and a short DHCP lease.

### Room for growth

All networks have at least twice as many free addresses as are in use today. The numbers `10.5.76.x – 10.5.78.x` and everything above `10.5.80.x` are free for new VLANs (for example a separate VLAN for a second office or for IoT/building control).
