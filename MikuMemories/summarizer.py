from gensim.summarization import summarize

def generate_summary(text, ratio=0.2):
    return summarize(text, ratio=ratio)