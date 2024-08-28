import ollama
import chromadb

documents = [
  "Clinic opens in morning from 9AM to 1PM, Mondays to Saturdays",
  "Clinic opens in evening from 6PM to 8PM, Mondays to Wednesdays only",
  "Clinic doesnt open in evening on Thursdays and Fridays",
  "Clinic doesnt open on Sundays"
  "Phone at reception is only answered during clinic timings.",
  "Emergency services are only available for booked patients.",
  "Doctors are not available on phone, social media or internet channels like WhatsApp, Messenger etc",
]

client = chromadb.Client()
collection = client.create_collection(name="docs")

# store each document in a vector embedding database
for i, d in enumerate(documents):
  response = ollama.embeddings(model="mxbai-embed-large", prompt=d)
  embedding = response["embedding"]
  collection.add(
    ids=[str(i)],
    embeddings=[embedding],
    documents=[d]
  )

  # an example prompt
prompt = "Is Clinic open in evening today?"

# generate an embedding for the prompt and retrieve the most relevant doc
response = ollama.embeddings(
  prompt=prompt,
  model="mxbai-embed-large"
)
results = collection.query(
  query_embeddings=[response["embedding"]],
  n_results=1
)
data = results['documents'][0][0]

# generate a response combining the prompt and data we retrieved in step 2
output = ollama.generate(
  model="llama2",
  prompt=f"You are an assistant who guides the patients answering their questions in succinct way. Using this data: {data} respond to this prompt: {prompt}. If question is about the day of the week, today is Friday; use this fact when finalizing the answer."
)

print(output['response'])