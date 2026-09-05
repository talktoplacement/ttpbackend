# CareerPlatform Deployment Guide

## Architecture

```
                        ┌─────────────────────────────────────────┐
                        │  Browser                                │
                        └──────────────────┬──────────────────────┘
                                           │ HTTPS
                     ┌─────────────────────┴──────────────────────┐
                     ▼                                            ▼
             ┌───────────────┐                          ┌────────────────────┐
             │   Vercel      │  fetch  ────────────►    │  Cloudflare (proxy)│
             │  (Next.js 16) │                          │       ▼            │
             └───────────────┘                          │   Nginx :443       │
                                                        │       ▼            │
                                                        │   backend :8080    │  ← Hostinger VPS
                                                        │   (Docker)         │
                                                        └────────┬───────────┘
                                                                 │
                        ┌────────────────────┬───────────────────┼───────────────────┐
                        ▼                    ▼                   ▼                   ▼
                ┌──────────────┐    ┌──────────────┐    ┌────────────────────┐
                │  Supabase    │    │  Razorpay    │    │  Brevo (all email) │
                │  Postgres 16 │    │  Payments    │    │  OTP + reset + adm │
                └──────────────┘    └──────────────┘    └────────────────────┘
```

| Piece | Service | Tier |
|---|---|---|
| Frontend | Vercel | Free |
| Backend | Hostinger VPS (2 GB RAM) — Docker | Paid |
| Database | Supabase (Postgres 16) | Free |
| Payments | Razorpay | — |
| Email (all) | Brevo (transactional API) | Free — 300/day |
| DNS / CDN | Cloudflare (recommended, proxied) | Free |
| Repo / CI | GitHub | Free |

**VPS app directory:** `/root/careerplatform/`

Why Docker for a single container? Reproducible builds + memory limits + `docker compose logs` beats fighting `journalctl`, and switching hosts later is a one-command move.

---

## Part 1 — Supabase (Database)

### 1.1 Create the project

1. supabase.com → New project. Region: pick the one closest to your VPS (Mumbai for Hostinger India).
2. Save the **database password** shown at creation time. You cannot retrieve it later.

### 1.2 Grab the connection info

Supabase Dashboard → **Project Settings → Database → Connection Info**.

You'll see three modes. Use the **Session pooler** (port `5432` on the pooler hostname). Npgsql uses prepared statements which the transaction pooler (`6543`) doesn't support.

Copy these values for `.env.prod` later:

| Env var | Value on Supabase page |
|---|---|
| `SUPABASE_HOST` | `aws-1-<region>.pooler.supabase.com` |
| `SUPABASE_PORT` | `5432` |
| `SUPABASE_DB` | `postgres` |
| `SUPABASE_USER` | `postgres.<PROJECT_REF>` (has the ref suffix) |
| `SUPABASE_PASSWORD` | The password from step 1.1 |

### 1.3 Apply the schema

From your laptop:
```bash
# Install psql if you don't have it:  brew install libpq  (or apt install postgresql-client)
psql "postgresql://postgres.<PROJECT_REF>:<PASSWORD>@aws-1-<region>.pooler.supabase.com:5432/postgres" \
     -f backend/schema.sql
```

All DDL in `schema.sql` is idempotent (`CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`) so re-running is safe.

Verify:
```bash
psql "postgresql://postgres.<PROJECT_REF>:<PASSWORD>@aws-1-<region>.pooler.supabase.com:5432/postgres" \
     -c "\dt"
```

### 1.4 Free-tier gotchas

- Supabase pauses inactive projects after 7 days. If your API health check starts failing, log into Supabase and unpause.
- 500 MB storage cap, 2 GB egress/month. Enough for early production; watch the dashboard.
- No automated backups on free tier. Add a nightly `pg_dump` on the VPS (see "Backups" below).

---

## Part 2 — Brevo (all outbound email)

Brevo handles every email the app sends — OTP, password reset, and admin/promotional. One
provider, one API key, one deliverability dashboard.

1. brevo.com → sign up. Free tier = 300 emails/day.
2. **Senders & IPs → Add a sender** → verify an email like `noreply@your-domain.com` (or use a Gmail address to start). All outbound mail is sent as this address.
3. **SMTP & API → API Keys → Generate**. Copy the key (starts with `xkeysib-`).
4. **Optional (OTP only)**: **Transactional → Email → Templates → New**. Use `{{ params.code }}`, `{{ params.ttlMinutes }}`, `{{ params.name }}` as placeholders. Note the numeric template ID. If you skip this, the backend uses a built-in HTML fallback for OTP too. Password-reset and promotional emails always use inline HTML composed by the sending code (no template).

Save for `.env.prod`:
- `BREVO_API_KEY`
- `BREVO_SENDER_EMAIL` (must match a verified sender)
- `BREVO_SENDER_NAME` (any string — appears as the "From" name)
- `BREVO_OTP_TEMPLATE_ID` (optional; OTP only)

---

## Part 3 — Razorpay

1. razorpay.com → sign up → complete KYC.
2. **Settings → API Keys → Generate Test/Live Keys**. Copy Key ID + Key Secret.
3. Live keys require KYC to be approved. For pre-launch use Test keys.

Save:
- `RAZORPAY_KEY_ID` (`rzp_test_...` or `rzp_live_...`)
- `RAZORPAY_KEY_SECRET`

---

## Part 4 — Backend on Hostinger VPS

### 4.1 SSH + baseline

```bash
ssh root@YOUR_VPS_IP
apt update && apt upgrade -y
timedatectl set-timezone Asia/Kolkata
```

### 4.2 Add swap (2 GB) — do this on a 2 GB box, no excuses

```bash
fallocate -l 2G /swapfile
chmod 600 /swapfile
mkswap /swapfile
swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab

sysctl vm.swappiness=10
echo 'vm.swappiness=10' >> /etc/sysctl.conf
free -h
```

### 4.3 Install Docker + Compose

```bash
apt install -y ca-certificates curl gnupg
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | \
  gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
  > /etc/apt/sources.list.d/docker.list

apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

docker --version && docker compose version
```

### 4.4 Install Nginx + Postgres client

```bash
apt install -y nginx postgresql-client-16
systemctl enable nginx
```

### 4.5 Clone repo

Use a GitHub Deploy Key if the repo is private:
```bash
ssh-keygen -t ed25519 -C "vps-deploy" -f /root/.ssh/id_ed25519 -N ""
cat /root/.ssh/id_ed25519.pub
# → paste into GitHub → repo → Settings → Deploy keys → Add (read-only is fine)
```

Then:
```bash
mkdir -p /root/careerplatform
cd /root/careerplatform
git clone git@github.com:YOUR_ORG/YOUR_REPO.git .
```

### 4.6 Create `.env.prod`

```bash
cp .env.prod.example .env.prod
nano .env.prod
```

Fill every value from Parts 1-3. Two things people usually get wrong:

- **`JWT_SECRET`** — 32+ random bytes: `openssl rand -base64 48`
- **`SUPABASE_PASSWORD`** — if your password contains special characters like `#`, `!`, `@`, `$`, they'll be interpreted by the shell when compose expands the env-var. If you hit connection errors, regenerate the password to alphanumeric in Supabase Dashboard → Database → Reset database password.

### 4.7 Build + start

```bash
cd /root/careerplatform
docker compose -f docker-compose.prod.yml --env-file .env.prod build
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d
docker compose -f docker-compose.prod.yml ps
```

Watch logs until you see `Now listening on: http://[::]:8080`:
```bash
docker compose -f docker-compose.prod.yml logs -f backend
```

Health check from inside the VPS:
```bash
curl http://127.0.0.1:8080/health/live      # → 200 {"status":"Healthy"}
curl http://127.0.0.1:8080/health/ready     # → 200 with per-dependency status
```

If `/health/ready` shows `Degraded` for Postgres, the Supabase connection string is wrong. Check the exact host / user / password in `.env.prod`.

### 4.8 Cloudflare Origin certificate

Cloudflare Dashboard → **SSL/TLS → Origin Server → Create Certificate**. Defaults (RSA 2048, 15 years, `*.your-domain.com, your-domain.com`) are fine.

On the VPS:
```bash
mkdir -p /etc/ssl/cloudflare
nano /etc/ssl/cloudflare/origin.pem       # paste certificate
nano /etc/ssl/cloudflare/origin-key.pem   # paste private key
chmod 600 /etc/ssl/cloudflare/origin-key.pem
```

### 4.9 Nginx config

```bash
rm -f /etc/nginx/sites-enabled/default
nano /etc/nginx/sites-available/api.your-domain.com
```

Paste (replace `your-domain.com`):

```nginx
server {
    listen 80;
    server_name api.your-domain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.your-domain.com;

    ssl_certificate     /etc/ssl/cloudflare/origin.pem;
    ssl_certificate_key /etc/ssl/cloudflare/origin-key.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;

    client_max_body_size 20M;

    # CORS is handled by the backend (Cors__AllowedOrigins). Don't add duplicate
    # Access-Control-* headers here or preflight will fail with duplicate values.

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 120s;
    }
}
```

```bash
ln -sf /etc/nginx/sites-available/api.your-domain.com /etc/nginx/sites-enabled/
nginx -t
systemctl reload nginx
```

### 4.10 Firewalls (both layers)

**UFW on host:**
```bash
ufw reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

Port `8080` stays internal — compose binds it to `127.0.0.1` so it's already unreachable from outside.

**Hostinger panel → VPS → Firewall rules:** create a profile allowing TCP `22`, `80`, `443`, activate on your VPS.

### 4.11 Cloudflare DNS

| Type | Name | Target | Proxy |
|---|---|---|---|
| A | `api` | `YOUR_VPS_IP` | Proxied (orange) |

SSL/TLS mode → **Full**.

### 4.12 Verify

```bash
# On the VPS
curl -k https://127.0.0.1/health/live -H "Host: api.your-domain.com"

# From your laptop
curl https://api.your-domain.com/health/live
# → {"status":"Healthy"}
```

---

## Part 5 — Frontend on Vercel

### 5.1 Import the repo

vercel.com → **Add New → Project** → import your GitHub repo → **Root Directory: `frontend`** → framework auto-detects Next.js 16.

### 5.2 Environment variables

Project → **Settings → Environment Variables**. Add for all three environments (Production, Preview, Development):

| Key | Value |
|---|---|
| `NEXT_PUBLIC_API_URL` | `https://api.your-domain.com` |

Any `NEXT_PUBLIC_*` value change requires a redeploy — Next bakes them into the client bundle at build time.

### 5.3 Custom domain

Project → **Settings → Domains** → add `your-domain.com` and `www.your-domain.com`. Vercel will show you the CNAME to point at (`cname.vercel-dns.com`). Add that CNAME in Cloudflare and set the record to **DNS only (grey cloud)** — Vercel needs to see the real request to issue its cert.

### 5.4 CORS wiring — the one thing that goes wrong

After the first Vercel deploy your production URL is either your custom domain or `your-app.vercel.app`. Copy that into `.env.prod` on the VPS as `FRONTEND_ORIGIN`, then:

```bash
cd /root/careerplatform
nano .env.prod        # set FRONTEND_ORIGIN=https://your-domain.com
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --force-recreate backend
```

If preview deployments should also hit prod API, set `FRONTEND_ORIGIN_PREVIEW=https://*-your-org.vercel.app` — but note the backend's CORS options list uses exact matches, not wildcards. For wildcard previews you'd need a code change; simpler to just add the specific preview URL you're testing to the origin list.

---

## Daily Operations

| Action | Command |
|---|---|
| Start backend | `docker compose -f docker-compose.prod.yml --env-file .env.prod up -d` |
| Stop backend | `docker compose -f docker-compose.prod.yml down` |
| Restart | `docker compose -f docker-compose.prod.yml restart backend` |
| Live logs | `docker compose -f docker-compose.prod.yml logs -f backend` |
| Last 100 lines | `docker compose -f docker-compose.prod.yml logs --tail=100 backend` |
| Container status | `docker compose -f docker-compose.prod.yml ps` |
| Memory usage | `docker stats --no-stream` |
| Health check | `curl http://127.0.0.1:8080/health/ready \| jq` |
| Nginx reload | `systemctl reload nginx` |
| Nginx test config | `nginx -t` |

---

## Deploy New Code

```bash
cd /root/careerplatform
git pull
docker compose -f docker-compose.prod.yml --env-file .env.prod build backend
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d
docker compose -f docker-compose.prod.yml logs -f --tail=100 backend
```

The health-check gate keeps traffic on the old container until the new one reports healthy — effectively a rolling restart of one.

Frontend deploys are automatic: Vercel picks up every push to `main` and every PR gets a preview URL.

Free build cache after several deploys:
```bash
docker system prune -a          # removes stopped containers + unused images
```

---

## Backups (Supabase → VPS)

Free-tier Supabase doesn't include automated backups. Nightly `pg_dump` from the VPS covers you:

```bash
mkdir -p /root/backups
crontab -e
```

Add:
```
0 2 * * * PGPASSWORD='YOUR_SUPABASE_PASSWORD' pg_dump -h aws-1-<region>.pooler.supabase.com -p 5432 -U postgres.<PROJECT_REF> -d postgres -F c -f /root/backups/careerplatform_$(date +\%F).dump 2>> /var/log/careerplatform-backup.log
5 2 * * * find /root/backups -name 'careerplatform_*.dump' -mtime +14 -delete
```

Restore into Supabase:
```bash
PGPASSWORD='YOUR_SUPABASE_PASSWORD' pg_restore \
  -h aws-1-<region>.pooler.supabase.com -p 5432 \
  -U postgres.<PROJECT_REF> -d postgres \
  --clean --no-owner --no-acl \
  /root/backups/YOURFILE.dump
```

Better yet, `rclone` the dumps to a free Cloudflare R2 or Backblaze B2 bucket so a VPS loss doesn't take backups with it.

---

## Schema Changes

Docker doesn't touch Supabase — the compose file has no `initdb`-style hook for a remote DB. Workflow:

1. Edit `backend/schema.sql` locally (idempotent DDL).
2. Commit + push.
3. Apply to Supabase from your laptop OR from the VPS:
   ```bash
   PGPASSWORD='YOUR_SUPABASE_PASSWORD' psql \
     -h aws-1-<region>.pooler.supabase.com -p 5432 \
     -U postgres.<PROJECT_REF> -d postgres \
     -f backend/schema.sql
   ```
4. Rebuild + redeploy backend (EF's `AppDbContext` may need to see the new DbSet):
   ```bash
   cd /root/careerplatform && git pull
   docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build backend
   ```

---

## Troubleshooting

**Cloudflare 502 / 522** — backend down or unreachable from nginx:
```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail=100 backend
systemctl status nginx
```

**`/health/ready` shows `Degraded` for Postgres** — Supabase connection failing:
```bash
# Test the connection from the VPS directly
PGPASSWORD='YOUR_PASSWORD' psql \
  -h aws-1-<region>.pooler.supabase.com -p 5432 \
  -U postgres.<PROJECT_REF> -d postgres -c 'SELECT 1;'
```
If this works but the backend doesn't, the env vars in `.env.prod` don't match. Compare with `docker compose exec backend env | grep Connection`.

**Backend startup fails with "Options validation failed"** — a `[Required]` config value is empty. Check `docker compose logs backend` — the message names the property. Usually `Jwt__Secret`, `Brevo__ApiKey`, or `Brevo__SenderEmail`.

**CORS blocked in browser** — `FRONTEND_ORIGIN` doesn't exactly match the origin the browser sends:
- Trailing slash mismatch (`https://a.com/` vs `https://a.com`) will fail — no trailing slash.
- HTTP vs HTTPS mismatch.
- `www.` prefix mismatch. Add both variants as `FRONTEND_ORIGIN` and `FRONTEND_ORIGIN_PREVIEW`.
- Rebuild+restart backend after any change.

**Vercel preview deploys can't reach the API** — either add the specific preview URL to backend CORS, or use Vercel's "Protect Preview Deployments" so previews stay internal.

**Supabase paused** — free-tier projects pause after 7 days of no activity. Log in to Supabase and unpause. Add an uptime monitor (UptimeRobot free) hitting `https://api.your-domain.com/health/live` every 5 min to keep the app AND Supabase active.

**OOM kill on VPS**:
```bash
dmesg | grep -i "killed process"
docker stats --no-stream
```
Lower `DOTNET_GCHeapHardLimit` in `docker-compose.prod.yml` (0x18000000 = 384 MB; try 0x10000000 = 256 MB).

**Razorpay signature verification fails** — using test keys in production or vice-versa. `rzp_test_*` and `rzp_live_*` sign differently.

**Brevo OTP not delivering** — check Brevo dashboard → **Transactional → Logs**. Common causes: sender not verified, spam trap (Gmail deliverability), template ID typo. The backend log shows the exact Brevo response body when it fails.

---

## Key Reminders

1. **Only port 22, 80, 443 open externally.** Backend (8080) is bound to `127.0.0.1` inside the compose file.
2. **UFW + Hostinger panel** both need those three ports — miss one and you get 522.
3. **Cloudflare SSL = Full**, not Full Strict (origin cert isn't from a public CA).
4. **Supabase session pooler, port 5432** — NOT transaction pooler (6543). Prepared statements break otherwise.
5. **Supabase passwords: alphanumeric only** to avoid shell expansion inside compose env var substitution.
6. **Any `NEXT_PUBLIC_*` change needs a Vercel redeploy** — those are baked into the client bundle.
7. **CORS origins are exact matches** — no wildcards. `https://your-domain.com` != `https://your-domain.com/`.
8. **Backups aren't automatic on Supabase free tier.** Wire up the cron above.
9. **Rotate `JWT_SECRET` on any suspected leak** — every existing token becomes invalid, so users need to re-login. That's the point.

---

## Auto-Deploy (optional)

Same GitHub webhook pattern as the friend's guide, but calling docker compose:

`/root/careerplatform/deploy.sh`:
```bash
#!/bin/bash
set -e
LOG=/var/log/careerplatform-deploy.log
echo "=== Deploy started at $(date) ===" >> "$LOG"
cd /root/careerplatform
git pull origin main >> "$LOG" 2>&1
docker compose -f docker-compose.prod.yml --env-file .env.prod build backend >> "$LOG" 2>&1
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d >> "$LOG" 2>&1
sleep 25
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:8080/health/live)
if [ "$STATUS" = "200" ]; then
  echo "✅ Deploy SUCCESS at $(date)" >> "$LOG"
else
  echo "❌ Deploy FAILED (health=$STATUS) at $(date)" >> "$LOG"
fi
```

Install `webhook`, drop `/etc/webhook.json` from the friend's guide (change `execute-command` to `/root/careerplatform/deploy.sh`), and add a `/hooks/` location in the nginx block proxying to `127.0.0.1:9000`. GitHub webhook URL: `https://api.your-domain.com/hooks/deploy-careerplatform`.

---

## Local Development

`docker-compose.yml` (dev) runs a local Postgres so you're not dependent on Supabase for coding:

```bash
docker compose up -d                                     # local Postgres :5432
dotnet run --project backend/src/CareerPlatform.Api     # backend :5215
npm --prefix frontend run dev                           # frontend :3000
```

To point local dev at Supabase instead, put the Supabase connection string in `backend/src/CareerPlatform.Api/appsettings.Development.json` (git-ignored) or use `dotnet user-secrets`.
