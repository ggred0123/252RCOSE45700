# NBA 4K 

Unity 기반 3D 농구 연습 / 트레이닝 게임 **NBA 4K** 레포지토리입니다.  
플레이어가 혼자 코트에서 다양한 농구 스킬을 연습할 수 있는 환경을 구현하는 것을 목표로 합니다.

---

## 🎯 프로젝트 개요

- **프로젝트명**: NBA 4K
- **장르**: 3D 농구 연습 / 트레이닝 게임
- **제작 동기**
  - 농구와 NBA 2K 시리즈에 대한 애정
  - 3D 게임 개발 및 Unity/Blender 파이프라인 경험 축적
- **기획 목표**
  - 1인 연습용 코트에서 자유롭게 움직이며
  - 드리블, 슛, 덩크, 페이드어웨이 등 다양한 농구 스킬을 연습할 수 있는 게임 구현

---

## 🕹️ 게임 컨셉 & 주요 기능

- **1인 연습 모드**
  - 플레이어가 코트 위를 자유롭게 이동하며 연습
- **연습 가능한 스킬**
  - 기본 드리블
  - 점프슛 (미완성)
  - 덩크 (미완성)
  - 페이드어웨이 (미완성)
- **득점 시스템** (구현 예정)
  - 골대에 공이 들어가면 2점 득점 처리
  - 상단 점수 UI에 실시간 반영
- **목표**
  - 현실감 있는 캐릭터 애니메이션
  - 자연스러운 농구공 물리 구현

---

## 🧰 기술 스택

- **엔진**: Unity 3D
- **언어**: C#
- **캐릭터 컨트롤**: Invector 3rd Person Controller (LITE)
- **UI**: TextMesh Pro 기반 점수 표시
- **렌더링**: Universal Render Pipeline (URP)

---

## 📁 프로젝트 구조 (요약)

- `Assets/`
  - `BasketPlayGround/` : 코트, 스타디움, 광고판 Prefab 및 Material
  - `Invector-3rdPersonController_LITE/` : 3인칭 캐릭터 컨트롤러 및 애니메이션
  - `Scripts/` : 공 제어, 점수 관리 등 사용자 정의 스크립트
  - 기타 캐릭터, 셰이더, 폰트 에셋 등
- `Packages/`
  - Unity 패키지 의존성 관리

---

## 🌍 메인 Scene 구성

- **환경**
  - 스타디움 / 코트 Prefab 배치
  - 라인, 페인트 존, 광고판 텍스처 적용
- **플레이어 캐릭터**
  - Invector 3rd Person Controller + 애니메이션 세트
- **농구공**
  - `Rigidbody` + `Collider` + `BallDribble` 스크립트
- **골대**
  - Hoop 콜라이더 및 태그 설정
  - 득점 판정 시스템 (미완성)
- **UI**
  - Canvas 상단에 TextMeshPro 텍스트로 점수 표시 예정

---

## Project Structure

- Assets/BasketPlayGround : 농구 코트, 골대 등 환경 에셋
- Assets/Invector-3rdPersonController_LITE : 3인칭 캐릭터 컨트롤러
- Assets/Scripts : 공 드리블, 점수 계산 등 게임 로직 스크립트
- Assets/Scenes/SampleScene.unity : 메인 게임 씬

---

## Main Scene

- Player : Unity-chan 캐릭터 + Invector 3rd Person Controller
- Ball : 물리 기반 드리블/슛이 적용되는 농구공
- Hoop : 림 충돌 판정 및 득점 로직이 연결된 골대
- UI : TextMesh Pro를 사용한 점수 표시

---

## Assets & Credits

- Character: Unity-chan (© Unity Technologies Japan / Unity-chan License)
- 3rd Person Controller: Invector Third Person Controller - Basic Locomotion FREE
- Fonts/UI: TextMesh Pro (Unity)

---

## 🔧 핵심 스크립트 – BallDribble (요약)

- **공 줍기**
  - 플레이어 주변에서 `F` 키 입력 시 공을 손에 쥐는 동작 처리
- **드리블**
  - 이동 속도에 따라 드리블 높이/주기 조절
  - 지면 충돌 체크를 통한 자연스러운 바운스 구현

---

## 🎞 모션 제작

- 현실감 있는 농구 모션 구현을 위해 **Blender** 사용
- Unity 애니메이션(`.fbx`)을 Blender로 가져와:
  - 골격을 프레임 단위로 조정
  - 실제 농구 경기 영상 참고
  - 뛰기, 점프 등 모션을 더 자연스럽게 보이도록 수정
- 드리블, 슛, 리바운드 등 다양한 동작을 추가 구현 중

---

## 📷 스크린샷

![게임 스크린샷](docs/screenshot.png)

---

## 조작법

- W/A/S/D : 캐릭터 이동
- 마우스 이동 : 카메라 회전
- Space : 점프
- F : 공 줍기
- E : 슛

---

## 🚀 앞으로의 계획

- **슈팅 기능 확장**
  - 점프슛: 키 입력 유지 시간 기반 차징, 골대 방향 포물선 궤적 계산
  - 페이드어웨이: 뒤로 물러나며 슛하는 애니메이션 및 성공 확률 조정
  - 덩크: 골대와의 거리/속도 조건을 이용한 덩크 가능 여부 판정
- **득점 시스템 & Score UI**
  - Hoop 콜라이더로 득점 여부 감지 후 2점 추가
  - `ScoreManager`를 통해 `"Score: N"` 형식으로 누적 점수 표시
- **애니메이션 디테일 보완**
  - 드리블/점프/착지 모션 보정
  - 캐릭터-공 상호작용 자연스러움 향상

> 중간 발표 기준 버전이며, 이후 구현/리팩토링에 따라 내용은 변경될 수 있습니다.
