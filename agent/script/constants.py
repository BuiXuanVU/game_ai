from enum import Enum

class NPCAction(Enum):
    CUI_CHAO = "CUI_CHAO"
    DI_BO = "DI_BO"
    QUY_LAY = "QUY_LAY"
    CHI_DUONG = "CHI_DUONG"
    LO_LANG = "LO_LANG"

class ItemType(Enum):
    GAO = "GAO"
    RUOU = "RUOU"
    CUOC = "CUOC"
    HAT_GIONG = "HAT_GIONG"

class Location(Enum):
    DAU_LANG = "DAU_LANG"
    DINH_LANG = "DINH_LANG"
    RUONG_LUA = "RUONG_LUA"
    NHA_LAO_NAM = "NHA_LAO_NAM"