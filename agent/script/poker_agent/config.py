import os
from pathlib import Path

from dotenv import load_dotenv


load_dotenv(Path(__file__).resolve().parents[2] / ".env")


class Settings:
    def __init__(self) -> None:
        self.host = os.getenv("POKER_AGENT_HOST", "127.0.0.1")
        self.port = int(os.getenv("POKER_AGENT_PORT", "8000"))
        self.lm_studio_base_url = os.getenv("LM_STUDIO_BASE_URL", "http://127.0.0.1:1234/v1")
        self.model_name = os.getenv("POKER_AGENT_MODEL") or os.getenv("GEMINI_MODEL", "google/gemma-3-4b")
        self.api_key = os.getenv("POKER_AGENT_API_KEY") or os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY", "not-needed")
        self.temperature = float(os.getenv("POKER_AGENT_TEMPERATURE") or os.getenv("GEMINI_TEMPERATURE", "0.2"))
        self.max_output_tokens = int(os.getenv("POKER_AGENT_MAX_OUTPUT_TOKENS") or os.getenv("GEMINI_MAX_OUTPUT_TOKENS", "256"))


settings = Settings()
