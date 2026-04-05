from poker_agent.models import DecisionRequest, DecisionResponse


class DecisionService:
    def __init__(self, llm_client) -> None:
        self.llm_client = llm_client

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        try:
            decision = self.llm_client.decide(request)
        except Exception as exc:
            return self._fallback(request, f"LLM request failed, fallback used. {exc}")
        return self._normalize(request, decision)

    def _normalize(self, request: DecisionRequest, decision: DecisionResponse) -> DecisionResponse:
        action = (decision.action or "").strip().capitalize()
        amount = max(0, decision.amount)

        if action not in {"Fold", "Check", "Call", "Raise"}:
            return self._fallback(request, "Model returned an unknown action, fallback used.")

        if action == "Check" and not request.canCheck:
            return self._fallback(request, "Check was illegal, fallback used.")

        if action == "Call" and not request.canCall:
            return self._fallback(request, "Call was illegal, fallback used.")

        if action == "Raise":
            if not request.canRaise:
                return self._fallback(request, "Raise was illegal, fallback used.")
            amount = min(max(amount, request.minRaiseAmount), request.maxRaiseAmount)
        else:
            amount = 0

        if action == "Fold" and not request.canFold:
            return self._fallback(request, "Fold was illegal, fallback used.")

        decision.action = action
        decision.amount = amount
        return decision

    def _fallback(self, request: DecisionRequest, reason: str) -> DecisionResponse:
        if request.canCheck:
            return DecisionResponse(action="Check", amount=0, reason=reason)
        if request.canCall:
            return DecisionResponse(action="Call", amount=0, reason=reason)
        return DecisionResponse(action="Fold", amount=0, reason=reason)
