import json
import re
from typing import Optional

from openai import OpenAI

from poker_agent.config import settings
from poker_agent.models import DecisionRequest, DecisionResponse
from poker_agent.prompt_builder import SYSTEM_PROMPT, build_user_prompt


class LmDecisionClient:
    def __init__(self, model_name: Optional[str] = None, base_url: Optional[str] = None) -> None:
        self.client = OpenAI(
            base_url=base_url or settings.lm_studio_base_url,
            api_key=settings.api_key,
        )
        self.model_name = model_name or settings.model_name

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        model_name = self.model_name
        if request.botProfile and request.botProfile.modelName:
            model_name = request.botProfile.modelName

        response = self.client.chat.completions.create(
            model=model_name,
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
