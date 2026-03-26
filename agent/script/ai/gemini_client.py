from openai import OpenAI
import json
from models import GameState
from dotenv import load_dotenv
from ai.prompt_builder import build_prompt
import os

load_dotenv(dotenv_path=r"script\key.env")

api_key = os.getenv("API_KEY")

client = OpenAI(
    api_key=os.getenv("API_KEY"),
    base_url="https://generativelanguage.googleapis.com/v1beta/openai/"
)


def call_gemini(state: GameState) -> int:
    prompt = build_prompt(state)

    try:
        response = client.chat.completions.create(
            model="gemini-2.5-flash",
            messages=[
                {
                    "role": "system",
                    "content": "You are a smart card game AI. Always return JSON only."
                },
                {
                    "role": "user",
                    "content": prompt
                }
            ],
            temperature=0.2
        )

        text = response.choices[0].message.content
        print("GEMINI RAW:", text)

        return extract_card_index(text)

    except Exception as e:
        print("Gemini error:", e)
        return -1


def extract_card_index(text: str) -> int:
    try:
        start = text.find("{")
        end = text.rfind("}")

        if start != -1 and end != -1:
            clean = text[start:end + 1]
            parsed = json.loads(clean)
            return parsed.get("cardIndex", -1)

    except Exception as e:
        print("Parse error:", e)

    return -1