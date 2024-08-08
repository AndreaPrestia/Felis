import json
import sseclient
import requests

# URL of the SSE endpoint
sse_url = 'https://localhost:7110/subscribe?topics=Test,TestAsync,TestError'

# Create a requests session
session = requests.Session()

# Connect to the SSE stream
response = session.get(sse_url, stream=True, verify=False)  # Set verify=True with valid certificates
client = sseclient.SSEClient(response)

# Listen for incoming messages
for event in client.events():
    try:
        json_object = json.loads(event.data)
        message_format = f'Received message - ${json_object.Id} with topic - ${json_object.Topic} with payload - ${json_object.Payload}'
        if json_object.topic == 'Test':
            print(message_format)
        if json_object.topic == 'TestAsync':
            print(message_format)
        else:
            raise Exception(message_format)  
    except Exception as ex:
            print(ex)

