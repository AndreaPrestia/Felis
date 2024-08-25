from datetime import datetime
import time
import ssl
import requests
from requests.adapters import HTTPAdapter

class SSLAdapter(HTTPAdapter):
    def __init__(self, pfx_path, pfx_password, *args, **kwargs):
        self.pfx_path = pfx_path
        self.pfx_password = pfx_password
        super().__init__(*args, **kwargs)

    def init_poolmanager(self, *args, **kwargs):
        context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
        context.load_pkcs12(self.pfx_path, self.pfx_password)
        kwargs['ssl_context'] = context
        return super().init_poolmanager(*args, **kwargs)

    def proxy_manager_for(self, *args, **kwargs):
        context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
        context.load_pkcs12(self.pfx_path, self.pfx_password)
        kwargs['ssl_context'] = context
        return super().proxy_manager_for(*args, **kwargs)

pfx_path = '../Output.pfx'
pfx_password = 'Password.1'

session = requests.Session()
session.mount('https://', SSLAdapter(pfx_path, pfx_password))
   
def publishMessage(topic):
    publish_url = f'https://localhost:7110/{topic}'

    presentDate = datetime.utcnow()
    unix_timestamp = datetime.timestamp(presentDate)*1000

    payload = {
        'description': f'{topic} at: {unix_timestamp} from Python publisher'
    }

    response = session.post(publish_url, payload, verify=False)  

    if response.status_code == 200:
        print('Message sent successfully')
    else:
        print(f'Failed to send message: {response.status_code} - {response.text}')
        
while True:
    publishMessage('Test')
    time.sleep(5)