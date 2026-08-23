import 'package:flutter_test/flutter_test.dart';
import 'package:real_stone_game/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('게임 제목을 표시한다', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const RealStoneApp());
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 200));
    expect(find.text('리얼 돌 키우기'), findsOneWidget);
    expect(find.textContaining('그라니아'), findsOneWidget);
    expect(find.textContaining('원석 상태'), findsOneWidget);
  });
}
