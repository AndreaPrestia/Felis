import json
import sseclient
import requests
import ssl
from requests.adapters import HTTPAdapter
from urllib3.poolmanager import PoolManager

def subscribe_to_stream(url):
    headers = {
        'Accept': 'application/x-ndjson'
    }

    try:
        with requests.get(url, headers=headers, stream=True) as response:
            # Ensure we have a successful connection
            response.raise_for_status()

            # Stream the response line by line
            for line in response.iter_lines():
                if line:
                    try:
                        # Decode and parse the JSON object
                        data = line.decode('utf-8')
                        json_object = json.loads(data)
                        message_format = f'Received message - ${json_object.Id} with topic - ${json_object.Topic} with payload - ${json_object.Payload}'
                        print(message_format)
                    except Exception as ex:
                        print(ex)
                   

    except requests.exceptions.RequestException as e:
        print(f"Error: {e}")

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

stream_url = 'https://localhost:7110/Test'

subscribe_to_stream(stream_url);


