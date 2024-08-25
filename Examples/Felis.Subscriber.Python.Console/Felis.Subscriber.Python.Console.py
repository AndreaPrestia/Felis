import json
import requests
from digital_certificate.cert import Certificate

def subscribe_to_stream(url):
    headers = {
        'Accept': 'application/x-ndjson'
    }

    try:
        with session.get(url, headers=headers, stream=True, cert=(_cert.pfxFile, _cert.private_key)) as response:
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

_cert = Certificate(
    pfx_file="../Output.pfx",
    password=b"Password.1"
)

_cert.read_pfx_file()

session = requests.Session()

stream_url = 'https://localhost:7110/Test'

subscribe_to_stream(stream_url);


