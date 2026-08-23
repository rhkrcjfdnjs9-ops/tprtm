import 'package:bridgebox/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('파일 크기를 읽기 쉽게 표시한다', () {
    expect(readableSize(1024), '1.0 KB');
    expect(readableSize(1048576), '1.0 MB');
  });

  test('경로에서 안전한 파일 이름만 남긴다', () {
    expect(cleanName(r'C:\photos\summer.jpg'), 'summer.jpg');
    expect(cleanName('../movie.mp4'), 'movie.mp4');
  });
}
