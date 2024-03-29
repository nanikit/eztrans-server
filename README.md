# eztrans-server

[![프로그램 창 이미지](doc/main-window.png)](##eztrans-server)

[AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)와 호환되는 이지트랜스 중개 프로그램입니다.
하는 일은 [이 파이썬 스크립트](https://github.com/HelloKS/ezTransWeb)와 비슷합니다.


## 사용법
1. 이지트랜스를 설치합니다.
2. [Ehnd](https://blog.naver.com/waltherp38/221062272423)를 설치합니다.
3. 실행할 AutoTranslator 설정파일(Config.ini)를 아래처럼 바꿔줍니다.
```
[Service]
Endpoint=CustomTranslate

[Custom]
Url=http://localhost:8000/
```
4. [EztransServer.Gui.exe](https://github.com/nanikit/eztrans-server/releases)를 받아 실행합니다.
5. 게임을 실행합니다.


## 증상 대처법
- To run this application, you must install .NET 메시지가 뜹니다.

[32비트용 .NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-6.0.1-windows-x86-installer)를 설치하고 다시 시도해주세요. 64비트는 통하지 않습니다.

- 이지트랜스를 찾을 수 없습니다

이지트랜스를 시험삼아 한번 실행해보세요.

- Ehnd 파일이 아닙니다

Ehnd를 최신 버전으로 설치해주세요.

- 창이 떴다 사라졌다 하면서 번역이 안 됩니다

Ehnd를 최신 버전으로 설치해주세요.

- 번역은 안 되고 서버 메시지가 뜨는 것도 없습니다

Config.ini가 서버 URL과 동일한지 확인해주세요.
