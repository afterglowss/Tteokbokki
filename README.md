<div align="center">

<img width="321" height="351" alt="군자떡볶이_로고" src="https://github.com/user-attachments/assets/3d9b3cad-c5a5-4ec7-b753-6aadca0c84cb" />

  # 🥘 군자 떡볶이 (Gunja Tteokbokki)
  **"군자 최고의 맛집, 그 사장님이 되어보세요!"**

  <br>

  <img src="https://img.shields.io/badge/Unity-6000.1-black?style=flat-square&logo=unity" />
  <img src="https://img.shields.io/badge/C%23-Language-239120?style=flat-square&logo=c-sharp" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" />
  <img src="https://img.shields.io/badge/Genre-Cooking%20Simulation-orange?style=flat-square" />

  <br><br>

  밀려드는 주문, 끓어오르는 육수, 그리고 매일매일의 정산 압박!<br>
  재료를 관리하고, 레시피에 맞춰 떡볶이를 조리하며,<br>
  가게를 성공적으로 운영해 해피 엔딩을 맞이하는 **요리 타이쿤 시뮬레이션 게임**입니다.

</div>

<br>

## 📅 Project Overview
* **프로젝트 명:** 군자 떡볶이 (Gunja Tteokbokki)
* **개발 기간:** 2025.07.01 ~ 2026.02.17 (약 8개월)
* **참여 인원:** 3명
* **장르:** 요리 시뮬레이션 / 경영 타이쿤
* **타겟 플랫폼:** PC (Windows)

<br>

## 👥 Contributors
<table width="100%">
  <tr>
    <td align="center" width="33%">
      <a href="https://github.com/afterglowss">
        <img src="https://avatars.githubusercontent.com/u/115690299?v=4" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/afterglowss"><b>신혜원</b></a><br>
      Role PM / Programming / Art
    </td>
    <td align="center" width="33%">
      <a href="https://github.com/Yoonha0">
        <img src="https://avatars.githubusercontent.com/u/162003763?v=4" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/Yoonha0"><b>윤하경</b></a><br>
      Role Design
    </td>
    <td align="center" width="33%">
      <a href="https://github.com/c-swim">
        <img src="https://avatars.githubusercontent.com/u/193379523?v=4" width="100" style="border-radius: 50%;">
      </a>
      <br>
      <b><a href="https://github.com/c-swim"><b>최영진</b></a><br>
      Role Programming
    </td>
  </tr>
</table>

<br>

## 🎮 Key Features

### 1. 🍳 리얼한 조리 시스템 (Cooking System)
> **"재료 투입부터 포장까지, 타이밍이 생명!"**
* **화구(Stove) 로직:** 각 화구마다 독립적인 타이머가 돌아가며, 재료(떡, 오뎅, 파 등)를 레시피에 맞게 투입해야 합니다.
* **상호작용:** 드래그 앤 드롭으로 재료를 냄비에 넣고, 다 익은 음식을 포장 용기로 옮기는 직관적인 조작을 구현했습니다.
* **실패 조건:** 레시피가 틀리면 폐기해야 하며, 영수증과 매칭에 실패할 경우 오배송의 디테일을 살렸습니다.

### 2. 🧾 주문 & 영수증 관리 (Order Management)
> **"쏟아지는 영수증을 처리하라!"**
* **랜덤 영수증 생성:** 손님마다 다른 메뉴와 추가 토핑(마라 소스, 로제 소스 등)이 적힌 영수증이 랜덤하게 생성됩니다.
* **매칭 시스템:** 포장된 음식과 영수증을 대조하여 성공/실패를 판정하고, 이에 따라 수익이 결정됩니다.
* **보너스 시스템:** '오늘의 보너스 재료'가 포함된 메뉴를 팔면 추가 수익을 얻을 수 있습니다.

### 3. 💰 경영 & 상점 (Shop & Economy)
> **"재료가 없으면 장사도 없다"**
* **재고 관리:** 매일 밤, 부족한 재료(떡, 소스 등)를 상점에서 주문해야 다음 날 영업이 가능합니다.
* **세금 & 정산:** 하루가 끝나면 매출에 따른 세금을 납부해야 하며, 재료가 소진되면 **조기 폐업**을 맞이할 수 있습니다.

### 4. 🎬 스토리 & 멀티 엔딩 (Story & Ending)
* **Yarn Spinner 활용:** NPC와의 대화 및 스토리 진행을 위해 Yarn Spinner를 도입했습니다.
* **분기 시스템:** 플레이어의 **최종 자산, 성공률, 해금한 재료 수**에 따라 Happy, Normal, Bad Ending 등 4가지의 다양한 결말이 기다립니다.

<br>

## 🛠️ Tech Stack

| Category | Technology |
| :--- | :--- |
| **Engine** | <img src="https://img.shields.io/badge/Unity-6000.1-black?style=flat-square&logo=unity" /> |
| **Language** | <img src="https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp" /> |
| **Animation** | <img src="https://img.shields.io/badge/DOTween-Pro-purple?style=flat-square" /> (UI 및 연출) |
| **Data & Save** | Newtonsoft.Json (JSON Serialization) |
| **Dialogue** | Yarn Spinner (스토리 스크립트) |
| **UI** | TextMeshPro (TMPro), UGUI |

<br>

## 📸 Screenshots

<details>
<summary><b>🖼️ 인게임 플레이 화면 보기 (Click)</b></summary>
<br>

| 메인 영업 화면 | 조리 및 포장 |
|:---:|:---:|
| <img width="1920" height="1080" alt="스크린샷 2026-02-19 011318" src="https://github.com/user-attachments/assets/d249a5bb-9172-4625-9694-65ac9cb882b2" /> | <img width="1920" height="1032" alt="image" src="https://github.com/user-attachments/assets/76cc18b4-76ba-40fe-b2cc-eac7dc4cb024" /> |
| **분주한 주방과 밀려드는 주문** | **정확한 레시피로 조리 성공!** |

| 상점 및 정산 | 엔딩 및 스토리 |
|:---:|:---:|
| <img width="1920" height="1080" alt="스크린샷 2026-02-19 011034" src="https://github.com/user-attachments/assets/4e757810-ba52-450f-b42b-0639686d6af7" /> | <img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/de115f4f-3571-4ace-9324-49c5d8701cec" /> |
| **내일 장사를 위한 재료 주문** | **당신의 선택이 만든 결말** |

</details>

<br>

## 🔧 Technical Challenges & Solutions

### 1. 세이브/로드 데이터 무결성 문제
* **문제 상황:** 게임 도중 강제 종료하거나 튜토리얼 직후 이어하기 시, 이전 데이터(좀비 데이터)가 남아 재료가 증발하거나 날짜가 꼬이는 현상 발생.
* **해결 방안:** `GameManager` 초기화 시점과 `SaveData.json`의 물리적 삭제(`DeleteSaveFile`) 로직을 명확히 분리. 특히 `StartSceneUI`에서 새 게임 시작 시 기존 세이브를 확실히 파기하도록 수정하여 해결함.

### 2. UI 연출과 데이터 갱신의 비동기 처리
* **문제 상황:** 마감 정산 창(`EndOfDayUIHandler`)이 켜질 때, 데이터 계산보다 애니메이션이 먼저 실행되어 0원으로 표기되거나 텍스트가 겹치는 문제.
* **해결 방안:** `DOTween` 시퀀스를 활용해 패널 등장 -> 데이터 계산 -> 텍스트 카운팅 애니메이션 순서로 실행 흐름을 제어하고, `Coroutine`을 통해 프레임 단위로 UI 갱신 타이밍을 조절함.

---
© 2026 Gunja Tteokbokki Team. All Rights Reserved.
