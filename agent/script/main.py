from fastapi import FastAPI
from models import GameState
from ai.gemini_client import call_gemini

app = FastAPI()

@app.post("/decide")
def decide(state: GameState):
    index = call_gemini(state)

    if index < 0 or index >= len(state.hand):
        return {"cardIndex": -1}

    return {"cardIndex": index}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=True)