# Use the official Node.js image
FROM node:18

# Create app directory
WORKDIR /src/Examples/Felis.Subscriber.NoAck.Node.Console

# Copy app source code
COPY package*.json ./
COPY . .

# Install dependencies
RUN npm install --production

# Run the Node.js console app
CMD ["node", "app.js"]
