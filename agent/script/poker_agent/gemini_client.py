import json
import re
from typing import Any

from google import genai
from google.genai import types

from poker_agent.config import settings
from poker_agent.models import DecisionRequest, DecisionResponse
from poker_agent.prompt_builder import SYSTEM_PROMPT, build_user_prompt


class GeminiDecisionClient:
    def __init__(self) -> None:
        if not settings.api_key:
            raise ValueError("Missing GEMINI_API_KEY or GOOGLE_API_KEY.")

        self.client = genai.Client(api_key=settings.api_key)

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        response = self.client.models.generate_content(
            model=settings.model_name,
            contents=build_user_prompt(request),
            config=types.GenerateContentConfig(
                temperature=settings.temperature,
                max_output_tokens=settings.max_output_tokens,
                response_mime_type="application/json",
                response_schema=DecisionResponse,
                system_instruction=SYSTEM_PROMPT,
            ),
        )

        if getattr(response, "parsed", None):
            decision = response.parsed
            if isinstance(decision, DecisionResponse):
                decision.rawResponse = response.text or ""
                return decision

        raw_text = self._extract_raw_text(response)
        payload = self._load_json(raw_text)
        decision = DecisionResponse.model_validate(payload)
        decision.rawResponse = raw_text
        return decision

    def _extract_raw_text(self, response: Any) -> str:
        if getattr(response, "text", None):
            return response.text

        texts: list[str] = []
        candidates = getattr(response, "candidates", None) or []
        for candidate in candidates:
            content = getattr(candidate, "content", None)
            parts = getattr(content, "parts", None) or []
            for part in parts:
                text = getattr(part, "text", None)
                if text:
                    texts.append(text)

        raw_text = "\n".join(texts).strip()
        if raw_text:
            return raw_text

        finish_reasons = []
        for candidate in candidates:
            reason = getattr(candidate, "finish_reason", None)
            if reason is not None:
                finish_reasons.append(str(reason))

        prompt_feedback = getattr(response, "prompt_feedback", None)
        raise ValueError(
            "Gemini returned no text content. "
            f"finish_reasons={finish_reasons or ['unknown']}, "
            f"prompt_feedback={prompt_feedback}"
        )

    def _load_json(self, raw_text: str) -> dict:
        if not raw_text.strip():
            raise ValueError("Gemini returned an empty response.")

        try:
            return json.loads(raw_text)
        except json.JSONDecodeError:
            match = re.search(r"\{.*\}", raw_text, re.DOTALL)
            if not match:
                raise
            return json.loads(match.group(0))
