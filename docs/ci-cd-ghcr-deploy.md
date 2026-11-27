# CI/CD and GHCR Deploy

這份說明對著現在這兩個 workflow：`.github/workflows/ci.yml` 負責建置、測試、把映像推上 GitHub Container Registry（GHCR）；`.github/workflows/deploy.yml` 負責登入一台已經裝好 Docker 的機器，把那兩個映像拉下來並啟動。

GHCR 是映像的倉庫。伺服器只拉已經推上去的映像來跑。正式環境要跑起來，映像必須先在 GHCR，部署用的 SSH 與密碼也必須先寫進 GitHub。映像的可見性跟著倉庫走；部署當下 Actions 用自己的 token 在伺服器上登入 GHCR，私有映像一樣拉得到。

## What runs when

`ci.yml` 在每次 `push` 和 `pull_request` 都會啟動。它先看這次改了哪些路徑，再決定要跑訂單服務、出貨服務，或兩個都跑。

| 你做的事 | 測試 | 推上 GHCR | 部署到伺服器 |
| --- | --- | --- | --- |
| 開 pull request，或推到不是預設分支的分支 | 有改到的那個服務會測 | 不推 | 不部署 |
| 推到預設分支（通常是 `main`） | 有改到的那個服務會測 | 有改到的那個服務會推 | 不部署 |
| 推一個 `v` 開頭的標籤，例如 `v1.2.0` | 兩個服務都測 | 兩個服務都推，標籤就是這個版本 | 兩個都推成功之後，用這個標籤部署 |
| 在 Actions 手動跑 Deploy | 不再測 | 不再推 | 拉你填的那個既有標籤 |

預設分支上的推送只更新映像。正式機器要換版本，用 `v*` 標籤，或是手動跑 Deploy 去拉一個已經存在的標籤。

`v*` 標籤會強制兩個服務都建、都推，就算這次 diff 只碰到其中一個。這樣伺服器上的兩張映像是同一版。

## Images and tags

帳號或組織名稱會轉成小寫。這份倉庫目前的擁有者是 `wenlianghuang`，映像名稱就是：

```text
ghcr.io/wenlianghuang/order-poc-order
ghcr.io/wenlianghuang/order-poc-shipment
```

每一次成功的發布都會帶短 SHA，七個字元，前面加上 `sha-`，例如 `sha-afd80f1`。

推上預設分支、而且這個 commit 仍是該分支的最先端時，同一張映像的 `latest` 會指到這次的 `sha-...`。後面又有新的 push 先完成時，這次就不會改 `latest`。

推 `v1.2.0` 時，同一張映像會同時有這幾個標籤：

- `sha-` 加七碼
- `1.2.0`
- `v1.2.0`

自動部署用的是標籤全名 `v1.2.0`，兩個服務都是這個名字。

映像是 `linux/amd64` 和 `linux/arm64` 各一份。x86 的雲端主機和 ARM 的機器都可以拉同一組標籤。

## What you type into GitHub

打開倉庫的 **Settings → Secrets and variables → Actions → Repository secrets**。名稱要完全一致，值貼進對應的 secret。`GITHUB_TOKEN` 不用自己建，Actions 會發。

一定要有，少一個的話這次部署會被跳過，workflow 仍算成功，伺服器不會動：

| Secret | 貼進去的內容 |
| --- | --- |
| `DEPLOY_HOST` | 伺服器位址，IP 或網域，例如 `203.0.113.10` |
| `DEPLOY_USER` | SSH 登入帳號，例如 `deploy` |
| `DEPLOY_SSH_KEY` | 這把部署專用私鑰的全文，含 `BEGIN` 和 `END` 兩行 |
| `POSTGRES_PASSWORD` | Postgres 密碼。不能有 `;`、`$`、引號、反斜線、空白或換行 |
| `AUTH_SIGNING_KEY` | JWT 簽章鑰匙，至少 32 bytes。不能有 `$`、引號、反斜線、空白或換行 |

可以不填，不填就用括號裡的預設：

| Secret | 不填時 |
| --- | --- |
| `DEPLOY_PORT` | `22` |
| `DEPLOY_PATH` | 該帳號家目錄下的 `order-poc` |
| `DEPLOY_KNOWN_HOSTS` | 第一次連線時接受伺服器的 host key |

`POSTGRES_PASSWORD` 和 `AUTH_SIGNING_KEY` 在本機產生再貼上。下面這兩行的輸出各自整段複製，不要加引號，也不要自己換行：

```bash
openssl rand -base64 24 | tr -d '/+='
openssl rand -base64 48 | tr -d '/+='
```

第一行當資料庫密碼，第二行當簽章鑰匙。

Deploy 這個 job 綁的環境名稱是 `production`。到 **Settings → Environments**，新增一個名字就叫 `production` 的環境。若在這個環境加上 Required reviewers，標籤推上去之後，部署會停在等你在 Actions 頁面按核准，兩張映像仍會先推上 GHCR。

## Prepare the server

伺服器要先有 Docker，而且 `docker compose version` 或 `docker-compose` 其中一個能跑。部署帳號要能直接下 `docker`，不必 `sudo`。Ubuntu 上把帳號放進 `docker` 群組之後，重新登入一次：

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose-v2
sudo usermod -aG docker deploy
```

在你自己的電腦做一把只給 Actions 用的鑰匙，不要用你平常登入的那把：

```bash
ssh-keygen -t ed25519 -f deploy_key -N ""
```

終端機會寫出 `deploy_key`（私鑰）和 `deploy_key.pub`（公鑰）。

公鑰放到伺服器上那個部署帳號的 `~/.ssh/authorized_keys`。私鑰全文貼進 GitHub 的 `DEPLOY_SSH_KEY`。貼完把本機的 `deploy_key` 收好，不要提交進倉庫。

要固定 host key 時，在本機對那台伺服器掃描，把輸出的那一行整行貼進 `DEPLOY_KNOWN_HOSTS`：

```bash
ssh-keyscan -p 22 203.0.113.10
```

埠不是 22 就把 `-p` 和 `DEPLOY_PORT` 改成同一個數字。

伺服器要能連到 `ghcr.io`。訂單服務對外是 `5064`，出貨服務對外是 `5065`。Postgres 只留在 Compose 的內部網路，宿主機上沒有對應埠。

## Release with a tag

在要上線的那個 commit 上打標籤，工作目錄保持乾淨。版本號用 `v` 加語意化版本，例如第一次上線：

```bash
git checkout main
git pull
git tag v1.0.0
git push origin v1.0.0
```

標籤打錯、還沒有人用這個版本部署時，刪掉本地和遠端再重打。已經部署過的版本不要重用同一個號碼：

```bash
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0
```

推上去之後打開 **Actions**。這次跑的是 **CI**。順序是測試、`publish order`、`publish shipment`，然後 **deploy**。deploy 的 log 裡會看到它寫出的映像名稱，形式是：

```text
ghcr.io/wenlianghuang/order-poc-order:v1.0.0
ghcr.io/wenlianghuang/order-poc-shipment:v1.0.0
```

映像清單在倉庫右側 **Packages**，或 `https://github.com/wenlianghuang?tab=packages`。點進 package 可以看到 `v1.0.0`、`1.0.0` 和 `sha-` 開頭的標籤。

下一個版本把上面的 `v1.0.0` 換成 `v1.1.0`、`v1.2.0`，同樣兩行：`git tag` 再 `git push origin` 那個標籤。

## Deploy an existing tag by hand

映像已經在 GHCR、只想讓伺服器改拉某一版時，不要再打標籤。打開 **Actions → Deploy → Run workflow**。

**Use workflow from** 留在預設分支。唯一要填的欄位是 `image_tag`。可填的值是 GHCR 上兩個映像都已經有的標籤，例如：

```text
v1.0.0
```

```text
sha-afd80f1
```

```text
latest
```

按 **Run workflow**。這個手動流程只部署，不會重新測試，也不會重新建映像。你填的字必須符合 `[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}`，所以用上面這種標籤即可。

`sha-...` 要兩個 package 都有同一個 SHA。只改到其中一個服務的那次預設分支推送，另一個服務不會產生這個 SHA。這種情況改填 `v1.0.0` 這種兩個服務一起推過的標籤。

## See that it is up

正式部署寫進伺服器的環境是 `Production`。Swagger 只在 `Development` 掛上，所以線上看 `http://伺服器:5064/swagger` 不會有頁面。用 API 本身確認。

Actions 的 deploy 綠了之後，在伺服器上：

```bash
cd ~/order-poc
docker compose --env-file .env -f docker-compose.prod.yml ps
```

`order`、`shipment`、`postgres` 都是 `running`，postgres 還要是 `healthy`。目錄裡會有 workflow 抄過去的 `docker-compose.prod.yml`、`up.sh`、`docker/postgres/init/create-shipments.sql` 和 `.env`。登入 GHCR 用的 token 在 `up.sh` 登入完就刪掉。

從你自己的電腦打訂單服務。把位址換成 `DEPLOY_HOST`：

```bash
curl -sS -D - -o /tmp/register.json \
  -X POST "http://203.0.113.10:5064/register" \
  -H "Content-Type: application/json" \
  -d '{"username":"staff","password":"practice"}'
```

第一次是 `201`，body 在 `/tmp/register.json`。同一個名稱再打一次是 `409`。接著登入：

```bash
curl -sS -X POST "http://203.0.113.10:5064/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"staff","password":"practice"}'
```

回應 `200`，JSON 裡有 `token`，這條部署就接到了訂單服務。出貨服務聽 `5065`，訂單付款之後才會由訂單服務在內部網路呼叫它，瀏覽器不用直接開這個埠也能完成部署檢查。

Postgres 的資料在 Docker volume `orderservice-pg`。同一台機器再部署新標籤會換掉兩個 API 容器，這份資料還在。`create-shipments.sql` 只在 volume 第一次建立時執行。

## When deploy is skipped

推 `v*` 之後 CI 是綠的，伺服器卻沒有換版本，打開 deploy job 的 **Check deploy secrets**。log 寫著 `DEPLOY_HOST, DEPLOY_USER, or DEPLOY_SSH_KEY is unset. Skipping deploy.` 就是三個 SSH secret 至少缺一個。補上之後用 **Deploy → Run workflow**，`image_tag` 填剛剛那個 `v1.0.0`，不必重打標籤。

`POSTGRES_PASSWORD` 或 `AUTH_SIGNING_KEY` 是空的、太短，或含有不允許的字元時，job 會在 **Write env file** 失敗，伺服器上的舊容器保持原樣。到 secret 改掉再開一次手動部署。

## What the server runs

`deploy.yml` 用 secret 組出 `.env`，再用 SSH 把檔案放到 `DEPLOY_PATH`，然後執行 `up.sh`。`up.sh` 做四件事：用這次 Actions 的 token 登入 `ghcr.io`、`pull` 兩個映像、`up -d`、登出。

伺服器上的 `.env` 由 workflow 產生，不會上傳倉庫裡的 `.env.prod`。`.env.prod` 只是本機對照 GHCR 映像名稱用的範本。
