import json
import sseclient
import requests

sse_url = 'https://localhost:7110/Test'

session = requests.Session()

response = session.get(sse_url, stream=True, verify=False)  # Set verify=True with valid certificates
client = sseclient.SSEClient(response)

for event in client.events():
    try:
        json_object = json.loads(event.data)
        message_format = f'Received message - ${json_object.Id} with topic - ${json_object.Topic} with payload - ${json_object.Payload}'
        print(message_format)
    except Exception as ex:
            print(ex)

