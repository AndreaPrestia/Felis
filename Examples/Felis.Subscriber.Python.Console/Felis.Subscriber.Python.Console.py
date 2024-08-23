import json
import sseclient
import requests
import ssl
from requests.adapters import HTTPAdapter
from urllib3.poolmanager import PoolManager

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

sse_url = 'https://localhost:7110/Test'

response = session.get(sse_url, stream=True, verify=False)
client = sseclient.SSEClient(response)

for event in client.events():
    try:
        json_object = json.loads(event.data)
        message_format = f'Received message - ${json_object.Id} with topic - ${json_object.Topic} with payload - ${json_object.Payload}'
        print(message_format)
    except Exception as ex:
            print(ex)

