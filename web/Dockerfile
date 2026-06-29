# syntax=docker/dockerfile:1

# ---- build ----
FROM node:22-alpine AS build
WORKDIR /src
RUN corepack enable
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile || pnpm install
COPY . .
RUN pnpm build

# ---- runtime (Nuxt SSR Nitro node server) ----
FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NITRO_PORT=3000 \
    NITRO_HOST=0.0.0.0
COPY --from=build /src/.output ./.output
EXPOSE 3000
CMD ["node", ".output/server/index.mjs"]
