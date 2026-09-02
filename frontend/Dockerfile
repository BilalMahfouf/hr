# ---- Build Stage ----
FROM node:24-alpine AS builder

RUN for i in 1 2 3; do npm install -g pnpm && break || sleep 10; done \
    && npm config set fetch-timeout 600000 \
    && npm config set fetch-retry-mintimeout 10000 \
    && npm config set fetch-retry-maxtimeout 60000 \
    && pnpm config set fetch-timeout 600000 \
    && pnpm config set fetch-retries 5

WORKDIR /app

COPY package.json pnpm-lock.yaml ./
RUN for i in 1 2 3; do pnpm install --frozen-lockfile && break || sleep 10; done

COPY . .

ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL

RUN pnpm approve-builds --all && pnpm build

# ---- Production Stage ----
FROM node:24-alpine AS runner

RUN for i in 1 2 3; do npm install -g serve && break || sleep 10; done

WORKDIR /app

# Only copy the built output, nothing else
COPY --from=builder /app/dist ./dist

EXPOSE 3000

CMD ["serve", "-s", "dist", "-l", "3000"]
