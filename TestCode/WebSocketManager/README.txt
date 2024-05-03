WebSocketManager.cs : 
BestHTTP Socket 에셋 사용 소켓 관리 클래스. (싱글턴)
소켓 Send, 디버그, Null 체크의 기본 기능 메소드 구성.

WebSocketManager_MyRoom.cs : 
WebSocketManager.cs의 partial 클래스.
Socket 추가 시 카테고리별 partial로 분리하여 관리.
외부 클래스 사용 시 호출하는 개별 메소드 및 값이 왔을 때 리턴해주는 delegate 구성.