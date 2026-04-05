import uvicorn

from poker_agent.config import settings


def main() -> None:
    uvicorn.run(
        "poker_agent.server:app",
        host=settings.host,
        port=settings.port,
        reload=False,
    )


if __name__ == "__main__":
    main()
