#!/usr/bin/env python3
"""
Buddy ShopAI Config Generator
自動產生租戶設定檔的互動式工具
"""

import json
import os
import sys
from pathlib import Path
from typing import Dict, List, Any


def print_header():
    """顯示標題"""
    print("\n" + "=" * 60)
    print("🎯 Buddy ShopAI Config Generator")
    print("=" * 60 + "\n")


def get_input(prompt: str, default: str = "") -> str:
    """取得使用者輸入，支援預設值"""
    if default:
        full_prompt = f"{prompt} [{default}]: "
    else:
        full_prompt = f"{prompt}: "
    
    value = input(full_prompt).strip()
    return value if value else default


def get_multiline_input(prompt: str) -> List[str]:
    """取得多行輸入（按空行結束）"""
    print(f"\n{prompt}")
    print("（每行輸入一項，完成後按 Enter 輸入空行）")
    lines = []
    while True:
        line = input("  - ").strip()
        if not line:
            break
        lines.append(line)
    return lines


def create_faq_item() -> Dict[str, Any]:
    """建立一個 FAQ 項目"""
    question = get_input("問題分類（如：運費與寄送）")
    answers = get_multiline_input("回答內容")
    
    return {
        "question": question,
        "answers": answers
    }


def generate_config_interactive() -> Dict[str, Any]:
    """互動式產生 config"""
    print("📋 開始建立租戶設定檔...\n")
    
    # 基本資訊
    print("--- 基本資訊 ---")
    tenant_id = get_input("租戶 ID（英文小寫，如 mrvshop）").lower()
    store_name = get_input("店名")
    business_hours = get_input("營業時間", "每日 10:00 - 21:00")
    bot_name = get_input("機器人暱稱", f"{store_name} AI 小幫手")
    contact_info = get_input("聯絡方式（如 Instagram: @shop）")
    
    # FAQ
    print("\n--- 常見問題設定 ---")
    print("請至少建立 3 個 FAQ 分類（運費、退換貨、查訂單等）\n")
    
    faq = []
    while True:
        print(f"\n📌 FAQ #{len(faq) + 1}")
        faq_item = create_faq_item()
        faq.append(faq_item)
        
        if len(faq) >= 3:
            more = get_input("\n是否新增更多 FAQ？(y/n)", "n").lower()
            if more != 'y':
                break
    
    # 回答風格
    print("\n--- 回答風格設定 ---")
    print("選擇語氣風格：")
    print("  1. 親切可愛")
    print("  2. 專業正式")
    print("  3. 潮流幽默")
    style_choice = get_input("請選擇 (1-3)", "1")
    
    style_map = {
        "1": "親切、可愛，使用繁體中文（台灣用語），可適度使用 Emoji。",
        "2": "專業、正式，使用繁體中文（台灣用語），保持禮貌。",
        "3": "潮流、幽默，使用繁體中文（台灣用語），可使用流行語與 Emoji。"
    }
    
    response_guidelines = [
        f"語氣：{style_map.get(style_choice, style_map['1'])}",
        "遇到不會的問題：請回答「這個問題我先幫您記下來，稍後會有專人為您服務喔！」",
        "格式：請勿使用 Markdown 語法，因為 LINE 顯示會亂掉，請用純文字或 Emoji 排版。"
    ]
    
    # 組合 config
    config = {
        "storeName": store_name,
        "businessHours": business_hours,
        "botName": bot_name,
        "contactInfo": contact_info,
        "faq": faq,
        "responseGuidelines": response_guidelines,
        "features": {
            "visionSearch": False,
            "richMenu": False
        }
    }
    
    return tenant_id, config


def save_config(tenant_id: str, config: Dict[str, Any]) -> str:
    """儲存 config 到檔案"""
    # 找到專案根目錄
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    config_dir = project_root / "configs"
    
    # 確保 configs 目錄存在
    config_dir.mkdir(exist_ok=True)
    
    # 儲存檔案
    output_path = config_dir / f"{tenant_id}.json"
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(config, f, ensure_ascii=False, indent=2)
    
    return str(output_path)


def preview_config(config: Dict[str, Any]):
    """預覽 config 內容"""
    print("\n" + "=" * 60)
    print("📄 Config 預覽")
    print("=" * 60)
    print(json.dumps(config, ensure_ascii=False, indent=2))
    print("=" * 60)


def load_from_questionnaire(file_path: str) -> Dict[str, Any]:
    """從問卷檔案載入（未來功能）"""
    # TODO: 實作從 Excel / CSV / JSON 問卷匯入
    pass


def main():
    """主程式"""
    print_header()
    
    # 選擇模式
    print("請選擇模式：")
    print("  1. 互動式輸入")
    print("  2. 從問卷檔案匯入（開發中）")
    mode = get_input("請選擇 (1-2)", "1")
    
    if mode == "1":
        tenant_id, config = generate_config_interactive()
    else:
        print("\n❌ 問卷匯入功能尚未實作，請使用互動式輸入。")
        return
    
    # 預覽
    preview_config(config)
    
    # 確認儲存
    confirm = get_input("\n是否儲存此設定？(y/n)", "y").lower()
    if confirm == 'y':
        output_path = save_config(tenant_id, config)
        print(f"\n✅ Config 檔案已儲存：{output_path}")
        print(f"\n下一步：")
        print(f"  1. 檢查 {output_path} 內容是否正確")
        print(f"  2. 執行部署：./scripts/deploy-all.sh {tenant_id}")
        print(f"  3. 在 LINE Developers Console 設定 Webhook URL")
    else:
        print("\n❌ 已取消儲存。")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⚠️  已中斷操作。")
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ 發生錯誤：{e}")
        sys.exit(1)
