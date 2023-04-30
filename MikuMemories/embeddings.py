import json
from sentence_transformers import SentenceTransformer

# Load the config.json file
with open("config.json", "r") as config_file:
    config = json.load(config_file)

# Read the "sentence_transformer" key from the config
def load_embedding_model():
    return SentenceTransformer(config["sentence_transformer"])

def generate_embedding(text):
    return model.encode([text])[0]
