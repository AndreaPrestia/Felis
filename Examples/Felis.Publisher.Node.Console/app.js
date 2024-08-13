'use strict';

const http2 = require('http2');

const publishMessage = async (topic, message) => {
    const endpoint = `https://localhost:7110/${topic}`;
    const credentials = Buffer.from("username:password").toString('base64');

    // Create a client session
    const client = http2.connect(endpoint, {
        rejectUnauthorized: false
    });

    const req = client.request({
        ':method': 'POST',
        'Content-Type': 'application/json',
        'Authorization': `Basic ${credentials}`
    });

    // Send the message body
    req.write(JSON.stringify(message));

    req.setEncoding('utf8');
   
    req.on('end', () => {
        client.close();
    });

    req.end();
}

const sleep = (ms) => {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

(async () => {
    try {
        console.log("Started Felis.Publisher.Node.Console");
        while (true) {
            publishMessage('Test', {
                description: `Test at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(10);

            publishMessage('TestAsync', {
                description: `TestAsync at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(55);

            publishMessage('TestError', {
                description: `TestError at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(2);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console ${e.message}`);
    }
})();



