# Postup na školu – server (VLAN 571) a klient (VLAN 570)

Pracovní tahák pro skupinu 2, není to finální text do README. Všechno, co je označené 📸, je screenshot, který potřebujeme do README (sekce 2.6 zadání).

> **Předpoklad:** v repu musí být `Dockerfile` a `docker-compose.yml` (úkol osoby A). Bez nich krok 4 nepůjde.

## 0. Nejdřív zjistit u učitele

- [ ] Na jaké virtualizační platformě VM děláme a jak se síťovka přiřadí do VLAN 571 / 570
- [ ] Jakou IP adresu má skupina 2 na VLAN 571 pro server (dál jako `SERVER_IP`, např. `10.5.71.x`)
- [ ] Jestli je gateway `10.5.71.1` / `10.5.70.1` a jaký DNS server použít
- [ ] Jestli má VLAN 571 přístup na internet (apt, Docker, GitHub)
- [ ] Jestli dostane klient na VLAN 570 IP z DHCP

## 1. Server VM – Ubuntu Server

Doporučené parametry: Ubuntu Server 24.04 LTS, 2 vCPU, 2–4 GB RAM, 20 GB disk, 1 síťovka ve **VLAN 571**.

Při instalaci:
- zaškrtnout **Install OpenSSH server**,
- síť buď rovnou nastavit staticky v instalátoru (Edit IPv4 → Manual), nebo až potom přes netplan (níže).

📸 Screenshot hotové instalace / přihlašovací obrazovky (`lsb_release -a` nebo `cat /etc/os-release`).

## 2. Statická IP (netplan)

```bash
ip a                      # zjistit název síťovky, např. ens18 / enp0s3
sudo nano /etc/netplan/50-cloud-init.yaml
```

```yaml
network:
  version: 2
  ethernets:
    ens18:                 # ← název z `ip a`
      dhcp4: false
      addresses:
        - 10.5.71.X/24     # ← SERVER_IP od učitele
      routes:
        - to: default
          via: 10.5.71.1   # ← ověřit u učitele
      nameservers:
        addresses: [1.1.1.1, 8.8.8.8]   # ← nebo školní DNS
```

```bash
sudo netplan try          # když se neodpojíš, potvrď Enterem
sudo netplan apply
ip a                      # 📸 IP 10.5.71.X/24
ip route                  # 📸 default via 10.5.71.1
ping -c 3 10.5.71.1       # gateway
```

YAML hlídá odsazení, používej **mezery, ne tabulátory**.

## 3. Docker

Oficiální repozitář Dockeru:

```bash
sudo apt update
sudo apt install -y ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Kdyby `download.docker.com` nešel, záloha z Ubuntu repa:
```bash
sudo apt install -y docker.io docker-compose-v2
```

Aby šel `docker` bez `sudo`:
```bash
sudo usermod -aG docker $USER
newgrp docker
```

Ověření:
```bash
docker --version          # 📸
docker compose version
docker run hello-world    # 📸 "Hello from Docker!"
```

## 4. Nasazení login platformy

```bash
git clone https://github.com/<ucet>/kvist-bjaelke-login.git
cd kvist-bjaelke-login
git checkout dev          # nebo main, podle toho, co bude finální
```

Pokud aplikace bere heslo prvního admina z proměnné prostředí, vytvoř `.env` vedle `docker-compose.yml` (**necommitovat**). Přesný název proměnné dodá osoba A:
```bash
nano .env                 # např. ADMIN_PASSWORD=...
```

```bash
docker compose up -d --build
docker ps                 # 📸 kontejner běží, PORTS 0.0.0.0:8080->8080/tcp
docker compose logs -f    # Ctrl+C pro konec, hledat "Now listening on: http://[::]:8080"
curl -I http://localhost:8080      # 📸 HTTP/1.1 200 nebo 302
```

Firewall přímo na serveru (ufw) je na Ubuntu defaultně vypnutý. Když je zapnutý (`sudo ufw status`):
```bash
sudo ufw allow OpenSSH
sudo ufw allow 8080/tcp
```

## 5. Klient VM – VLAN 570

Stačí Ubuntu Desktop nebo jakékoli OS s prohlížečem, síťovka ve **VLAN 570**.

```bash
ip a                      # 📸 IP 10.5.70.x/24 (Windows: ipconfig)
ip route                  # 📸 default via 10.5.70.1
```

Test spojení na server:
```bash
ping -c 3 SERVER_IP       # může NEPROJÍT – firewall pouští jen TCP 8000–8100, ne ICMP. To je v pořádku.
nc -zv SERVER_IP 8080     # 📸 "succeeded" / "open"
curl -I http://SERVER_IP:8080      # 📸
```

V prohlížeči `http://SERVER_IP:8080`:
- [ ] 📸 přihlašovací stránka (v adresním řádku musí být vidět IP a port)
- [ ] 📸 přihlášení jako **user**: vidí tabulku uživatelů, ale nemůže upravovat
- [ ] 📸 přihlášení jako **superuser**: vytvoří, upraví a smaže běžného usera
- [ ] 📸 přihlášení jako **admin**: spravuje i superusery a adminy
- [ ] 📸 špatné heslo → chybová hláška
- [ ] 📸 (bonus) user zkusí otevřít admin stránku přímo přes URL → Access denied

## 6. Časté problémy a co s nimi

Zapisujte si průběžně, co se reálně stalo, protože to chce sekce „challenges“ v README.

| Problém | Příčina | Řešení |
|---|---|---|
| `curl localhost:8080` na serveru jde, z klienta ne | Appka poslouchá jen na `localhost` uvnitř kontejneru | V kontejneru musí poslouchat na `0.0.0.0`/`[::]` (u .NET image je to default přes `ASPNETCORE_HTTP_PORTS=8080`) |
| Prohlížeč přesměruje na `https://…` a spadne | `app.UseHttpsRedirection()` v `Program.cs`, ale v kontejneru je jen HTTP | Spouštět HTTPS redirect jen v Development, nebo ho v kontejneru vypnout |
| `ping` z klienta na server nejde | Firewall pouští jen TCP 8000–8100 | Testovat přes `nc -zv` / `curl`, ne ping |
| `permission denied … docker.sock` | Uživatel není ve skupině `docker` | `sudo usermod -aG docker $USER` + odhlásit/přihlásit (`newgrp docker`) |
| Po restartu kontejneru zmizí uživatelé | SQLite `app.db` je uvnitř kontejneru | Namapovat volume na složku s databází v `docker-compose.yml` |
| `netplan apply` hlásí chybu | Špatné odsazení / tabulátory v YAML | Jen mezery, zkontrolovat `sudo netplan --debug generate` |
| VM nemá IP / žádné spojení | Síťovka VM není ve správné VLAN | Zkontrolovat nastavení síťového adaptéru VM u učitele |
| `apt` / `git clone` nejde | VLAN nemá internet nebo chybí DNS | Ověřit `ping 1.1.1.1` a pak `ping google.com`, zeptat se na proxy/DNS |
