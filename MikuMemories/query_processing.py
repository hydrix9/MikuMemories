import spacy
import gensim.downloader as api

def load_nlp_model():
    return spacy.load("en_core_web_sm")

def tokenize_input(nlp, user_input):
    return nlp(user_input)

def extract_entities(doc):
    return [ent.text for ent in doc.ents]

def expand_query(embedding_model, entities, topn):
    expanded_terms = []
    for term in entities:
        similar_terms = embedding_model.most_similar(positive=[term], topn=topn)
        expanded_terms.extend([t[0] for t in similar_terms])
    return " ".join(expanded_terms)

def process_query(embedding_model, nlp_model, user_input, query_expansion_topn):
    doc = tokenize_input(nlp_model, user_input)
    entities = extract_entities(doc)
    expanded_query = expand_query(embedding_model, entities, query_expansion_topn)

    return expanded_query

def create_nlp_doc(nlp, user_input):
    return nlp(user_input)

def extract_intents(doc):
    return [token.lemma_ for token in doc if token.pos_ == 'VERB']

def extract_entities(doc):
    return [(ent.text, ent.label_) for ent in doc.ents]