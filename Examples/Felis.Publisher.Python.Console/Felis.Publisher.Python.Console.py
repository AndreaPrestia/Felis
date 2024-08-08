
import base64
from datetime import datetime
from time import time
import httpx

def publishMessage(topic):
    publish_url = 'https://localhost:7110/publish'

    credentials = base64.urlsafe_b64encode(bytes('username:password', 'utf-8'))

    client = httpx.Client(verify=False) 

    headers = {
        'Authorization': f'Basic {credentials}', 
        'Content-Type': 'application/json'
    }

    presentDate = datetime.utcnow()
    unix_timestamp = datetime.timestamp(presentDate)*1000

    payload = {
        'topic': topic,
        'payload': f'{topic} at: {unix_timestamp} from Python publisher'
    }

    response = client.post(publish_url, json=payload, headers=headers)

    if response.status_code == 200:
        print('Message sent successfully')
    else:
        print(f'Failed to send message: {response.status_code} - {response.text}')
        
while True:
    publishMessage('Test')
    time.sleep(5)
    publishMessage('TestAsync')
    time.sleep(5)
    publishMessage('TestError')
    time.sleep(5)
