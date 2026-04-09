import logging
import traceback

from fastapi import FastAPI, HTTPException

from poker_agent.decision_service import DecisionService
#from poker_agent.gemini_client import GeminiDecisionClient
from poker_agent.llm_runtime import LmDecisionClient
from poker_agent.models import DecisionRequest, DecisionResponse


app = FastAPI(title="Poker Local Agent")
logger = logging.getLogger("poker_agent")

try:
    decision_service = DecisionService(LmDecisionClient())
except Exception as exc:
    decision_service = None
    startup_error = str(exc)
    logger.exception("Poker agent startup failed")
else:
    startup_error = ""


@app.get("/")
def root() -> dict:
    return {
        "service": "Poker Local Agent",
        "health": "/health",
        "decide": "/v1/decide",
    }


@app.get("/health")
def health() -> dict:
    return {
        "ok": decision_service is not None,
        "error": startup_error,
    }


@app.post("/v1/decide", response_model=DecisionResponse)
def decide(request: DecisionRequest) -> DecisionResponse:
    if decision_service is None:
        logger.error("Decision requested while agent is not configured: %s", startup_error)
        raise HTTPException(status_code=500, detail=startup_error or "Agent is not configured.")

    try:
        return decision_service.decide(request)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Decision request failed: %s", exc)
        logger.debug("Request payload: %s", request.model_dump_json())
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(exc)) from exc
