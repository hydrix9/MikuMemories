import json
from stegano import lsb

def encode_data_in_image(input_image_path, output_image_path, data):
    json_data = json.dumps(data)
    secret_image = lsb.hide(input_image_path, json_data)
    secret_image.save(output_image_path)

def decode_data_from_image(image_path):
    json_data = lsb.reveal(image_path)
    return json.loads(json_data)
