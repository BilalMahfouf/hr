# ---- Build Stage ----
FROM node:24-alpine AS builder

RUN npm install -g pnpm

WORKDIR /app

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
RUN pnpm build

# ---- Production Stage ----
FROM node:24-alpine AS runner

RUN npm install -g serve

WORKDIR /app

# Only copy the built output, nothing else
COPY --from=builder /app/dist ./dist

EXPOSE 3001

CMD ["serve", "-s", "dist", "-l", "3000"]
