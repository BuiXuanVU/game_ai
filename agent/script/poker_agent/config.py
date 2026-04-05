import os
from pathlib import Path

from dotenv import load_dotenv


load_dotenv(Path(__file__).resolve().parents[2] / ".env")


class Settings:
    def __init__(self) -> None:
        self.host = os.getenv("POKER_AGENT_HOST", "127.0.0.1")
        self.port = int(os.getenv("POKER_AGENT_PORT", "8000"))
        self.model_name = os.getenv("GEMINI_MODEL", "gemini-2.5-flash")
        self.api_key = os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY", "")
        self.temperature = float(os.getenv("GEMINI_TEMPERATURE", "0.2"))
        self.max_output_tokens = int(os.getenv("GEMINI_MAX_OUTPUT_TOKENS", "256"))


settings = Settings()
