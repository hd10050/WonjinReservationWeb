FROM node:24-alpine

WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci --legacy-peer-deps
COPY . .

EXPOSE 3700
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "3700"]
