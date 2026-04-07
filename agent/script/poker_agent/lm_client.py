import json
import re
from typing import Any

from openai import OpenAI

from poker_agent.config import settings
from poker_agent.models import DecisionRequest, DecisionResponse
from poker_agent.prompt_builder import SYSTEM_PROMPT, build_user_prompt


class LmDecisionClient:
    def __init__(self) -> None:
        # LM Studio không cần API key
        self.client = OpenAI(
            base_url="http://127.0.0.1:1234/v1",
            api_key="not-needed"
        )

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        response = self.client.chat.completions.create(
            model="google/gemma-3-4b",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": build_user_prompt(request)},
            ],
            temperature=settings.temperature,
            max_tokens=settings.max_output_tokens,
        )

        raw_text = response.choices[0].message.content

        payload = self._load_json(raw_text)
        decision = DecisionResponse.model_validate(payload)
        decision.rawResponse = raw_text
        return decision

    def _load_json(self, raw_text: str) -> dict:
        if not raw_text or not raw_text.strip():
            raise ValueError("LLM returned empty response.")

        try:
            return json.loads(raw_text)
        except json.JSONDecodeError:
            match = re.search(r"\{.*\}", raw_text, re.DOTALL)
            if not match:
                raise
            return json.loads(match.group(0))