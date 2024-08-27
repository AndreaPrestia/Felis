# Use the official Node.js image
FROM node:18

# Create app directory
WORKDIR /Examples/Felis.Publisher.Node.Console

# Copy app source code
COPY package.json .
# Install dependencies

RUN npm install
COPY . ./

# Run the Node.js console app
CMD ["node", "app.js"]
