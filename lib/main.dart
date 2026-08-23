import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:isolate';
import 'dart:math';

import 'package:crypto/crypto.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_video_thumbnail_plus/flutter_video_thumbnail_plus.dart';
import 'package:gal/gal.dart';
import 'package:http/http.dart' as http;
import 'package:image/image.dart' as image_lib;
import 'package:path_provider/path_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:webview_flutter/webview_flutter.dart';

const appName = 'BridgeBox';
const serverPort = 49832;

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const BridgeBoxApp());
}

class BridgeBoxApp extends StatelessWidget {
  const BridgeBoxApp({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
    debugShowCheckedModeBanner: false,
    title: appName,
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff3157d5)),
      useMaterial3: true,
      scaffoldBackgroundColor: const Color(0xfff5f6fa),
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
    ),
    home: Platform.isWindows ? const DesktopHome() : const MobileHome(),
  );
}

String cleanName(String value) {
  final name = value.replaceAll('\\', '/').split('/').last;
  return name.replaceAll(RegExp(r'[<>:"/\\|?*\x00-\x1f]'), '_').trim();
}

String readableSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1048576) return '${(bytes / 1024).toStringAsFixed(1)} KB';
  if (bytes < 1073741824) return '${(bytes / 1048576).toStringAsFixed(1)} MB';
  return '${(bytes / 1073741824).toStringAsFixed(2)} GB';
}

IconData fileIcon(String name) {
  final lower = name.toLowerCase();
  if (RegExp(r'\.(jpg|jpeg|png|gif|webp|heic)$').hasMatch(lower)) {
    return Icons.image;
  }
  if (RegExp(r'\.(mp4|mov|avi|mkv|webm)$').hasMatch(lower)) return Icons.movie;
  return Icons.insert_drive_file;
}

bool isImageFile(String name) => RegExp(
  r'\.(jpg|jpeg|png|gif|webp|heic|bmp)$',
  caseSensitive: false,
).hasMatch(name);

bool isVideoFile(String name) => RegExp(
  r'\.(mp4|mov|avi|mkv|webm|m4v)$',
  caseSensitive: false,
).hasMatch(name);

class SharedFile {
  const SharedFile(this.name, this.size, this.modified);
  final String name;
  final int size;
  final DateTime modified;
  factory SharedFile.fromJson(Map<String, dynamic> value) => SharedFile(
    value['name'] as String,
    value['size'] as int,
    DateTime.parse(value['modified'] as String),
  );
  Map<String, dynamic> toJson() => {
    'name': name,
    'size': size,
    'modified': modified.toIso8601String(),
  };
}

class DesktopServer {
  DesktopServer({required this.folder, required this.pin});
  Directory folder;
  String pin;
  HttpServer? _server;
  final events = StreamController<void>.broadcast();
  final Map<String, Future<File?>> _thumbnailJobs = {};
  bool get running => _server != null;

  Future<void> start() async {
    if (running) return;
    await folder.create(recursive: true);
    _server = await HttpServer.bind(InternetAddress.anyIPv4, serverPort);
    _server!.listen(_handle, onError: (_) => stop());
    events.add(null);
  }

  Future<void> stop() async {
    await _server?.close(force: true);
    _server = null;
    events.add(null);
  }

  Future<List<SharedFile>> files() async {
    final result = <SharedFile>[];
    if (!await folder.exists()) return result;
    await for (final entity in folder.list()) {
      if (entity is File && !entity.path.endsWith('.bridgepart')) {
        final stat = await entity.stat();
        result.add(
          SharedFile(cleanName(entity.path), stat.size, stat.modified),
        );
      }
    }
    result.sort((a, b) => b.modified.compareTo(a.modified));
    return result;
  }

  Future<File> _uniqueFile(String requested) async {
    final safe = cleanName(requested).isEmpty ? 'file' : cleanName(requested);
    var candidate = File('${folder.path}${Platform.pathSeparator}$safe');
    if (!await candidate.exists()) return candidate;
    final dot = safe.lastIndexOf('.');
    final stem = dot > 0 ? safe.substring(0, dot) : safe;
    final ext = dot > 0 ? safe.substring(dot) : '';
    var number = 2;
    while (await candidate.exists()) {
      candidate = File(
        '${folder.path}${Platform.pathSeparator}$stem ($number)$ext',
      );
      number++;
    }
    return candidate;
  }

  Future<int> addFiles(List<PlatformFile> pickedFiles) async {
    var added = 0;
    for (final picked in pickedFiles) {
      final target = await _uniqueFile(picked.name);
      final partial = File('${target.path}.bridgepart');
      final sink = partial.openWrite();
      try {
        await for (final chunk in picked.readAsByteStream()) {
          sink.add(chunk);
        }
        await sink.flush();
        await sink.close();
        await partial.rename(target.path);
        added++;
      } catch (_) {
        await sink.close();
        if (await partial.exists()) await partial.delete();
        rethrow;
      }
    }
    events.add(null);
    return added;
  }

  Future<File?> thumbnailFor(SharedFile sharedFile) {
    return _thumbnailJobs.putIfAbsent(sharedFile.name, () async {
      try {
        return await _createThumbnail(sharedFile);
      } finally {
        _thumbnailJobs.remove(sharedFile.name);
      }
    });
  }

  Future<File?> _createThumbnail(SharedFile sharedFile) async {
    if (!isImageFile(sharedFile.name) && !isVideoFile(sharedFile.name)) {
      return null;
    }
    final source = File(
      '${folder.path}${Platform.pathSeparator}${cleanName(sharedFile.name)}',
    );
    if (!await source.exists()) return null;
    final cacheDirectory = Directory(
      '${folder.path}${Platform.pathSeparator}.bridgebox-thumbnails',
    );
    await cacheDirectory.create(recursive: true);
    final thumbnail = File(
      '${cacheDirectory.path}${Platform.pathSeparator}${cleanName(sharedFile.name)}.jpg',
    );
    if (await thumbnail.exists()) {
      final thumbnailStat = await thumbnail.stat();
      if (!thumbnailStat.modified.isBefore(sharedFile.modified)) {
        return thumbnail;
      }
    }
    if (isImageFile(sharedFile.name)) {
      final sourcePath = source.path;
      final thumbnailPath = thumbnail.path;
      final created = await Isolate.run(() async {
        final decoded = image_lib.decodeImage(
          await File(sourcePath).readAsBytes(),
        );
        if (decoded == null) return false;
        final resized = decoded.width >= decoded.height
            ? image_lib.copyResize(decoded, width: 320)
            : image_lib.copyResize(decoded, height: 320);
        await File(
          thumbnailPath,
        ).writeAsBytes(image_lib.encodeJpg(resized, quality: 78), flush: true);
        return true;
      });
      return created ? thumbnail : null;
    }
    final bytes = await FlutterVideoThumbnailPlus.thumbnailData(
      video: source.path,
      imageFormat: ImageFormat.jpeg,
      maxHeight: 320,
      maxWidth: 320,
      timeMs: 1000,
      quality: 78,
    );
    if (bytes == null || bytes.isEmpty) return null;
    await thumbnail.writeAsBytes(bytes, flush: true);
    return thumbnail;
  }

  Future<void> _handle(HttpRequest request) async {
    try {
      request.response.headers.set('x-content-type-options', 'nosniff');
      if (request.headers.value('x-bridge-pin') != pin) {
        request.response.statusCode = HttpStatus.unauthorized;
        request.response.write('PIN mismatch');
      } else if (request.method == 'GET' && request.uri.path == '/health') {
        request.response.headers.contentType = ContentType.json;
        request.response.write(jsonEncode({'name': appName, 'ok': true}));
      } else if (request.method == 'GET' && request.uri.path == '/files') {
        request.response.headers.contentType = ContentType.json;
        request.response.write(
          jsonEncode((await files()).map((f) => f.toJson()).toList()),
        );
      } else if (request.method == 'GET' &&
          request.uri.pathSegments.length == 2 &&
          request.uri.pathSegments.first == 'thumbnail') {
        final name = cleanName(request.uri.pathSegments.last);
        final match = (await files()).where((file) => file.name == name);
        final thumbnail = match.isEmpty
            ? null
            : await thumbnailFor(match.first);
        if (thumbnail == null || !await thumbnail.exists()) {
          request.response.statusCode = HttpStatus.notFound;
        } else {
          request.response.headers.contentType = ContentType('image', 'jpeg');
          request.response.headers.set(
            'cache-control',
            'private, max-age=86400',
          );
          request.response.contentLength = await thumbnail.length();
          await request.response.addStream(thumbnail.openRead());
        }
      } else if (request.method == 'POST' && request.uri.path == '/upload') {
        final encoded = request.headers.value('x-file-name') ?? 'file';
        final target = await _uniqueFile(Uri.decodeComponent(encoded));
        final partial = File('${target.path}.bridgepart');
        final sink = partial.openWrite();
        try {
          await for (final chunk in request) {
            sink.add(chunk);
          }
          await sink.flush();
          await sink.close();
          await partial.rename(target.path);
          request.response.headers.contentType = ContentType.json;
          request.response.write(jsonEncode({'name': cleanName(target.path)}));
          events.add(null);
        } catch (_) {
          await sink.close();
          if (await partial.exists()) await partial.delete();
          rethrow;
        }
      } else if (request.method == 'GET' &&
          request.uri.pathSegments.length == 2 &&
          request.uri.pathSegments.first == 'file') {
        final name = cleanName(request.uri.pathSegments.last);
        final file = File('${folder.path}${Platform.pathSeparator}$name');
        if (!await file.exists()) {
          request.response.statusCode = HttpStatus.notFound;
        } else {
          request.response.contentLength = await file.length();
          request.response.headers.set(
            'content-disposition',
            "attachment; filename*=UTF-8''${Uri.encodeComponent(name)}",
          );
          await request.response.addStream(file.openRead());
        }
      } else {
        request.response.statusCode = HttpStatus.notFound;
      }
      await request.response.close();
    } catch (_) {
      try {
        request.response.statusCode = HttpStatus.internalServerError;
        await request.response.close();
      } catch (_) {}
    }
  }
}

class DesktopHome extends StatefulWidget {
  const DesktopHome({super.key});
  @override
  State<DesktopHome> createState() => _DesktopHomeState();
}

class _DesktopHomeState extends State<DesktopHome> {
  DesktopServer? server;
  List<SharedFile> files = [];
  String folderPath = '';
  String pin = '246810';
  String tailscaleIp = '확인 중...';
  String? error;
  bool listUnlocked = false;
  String? listPasswordHash;
  String? listPasswordSalt;
  StreamSubscription<void>? subscription;

  bool get listPasswordConfigured =>
      listPasswordHash != null && listPasswordSalt != null;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    final prefs = await SharedPreferences.getInstance();
    final downloads = await getDownloadsDirectory();
    folderPath =
        prefs.getString('folder') ??
        '${downloads?.path ?? Directory.current.path}${Platform.pathSeparator}BridgeBox';
    pin = prefs.getString('pin') ?? '246810';
    listPasswordHash = prefs.getString('list_password_hash');
    listPasswordSalt = prefs.getString('list_password_salt');
    tailscaleIp = await _findTailscaleIp() ?? 'Tailscale 주소를 찾지 못함';
    server = DesktopServer(folder: Directory(folderPath), pin: pin);
    subscription = server!.events.stream.listen((_) => _refresh());
    try {
      await server!.start();
      await _refresh();
    } catch (e) {
      error = '서버 시작 실패: $e';
    }
    if (mounted) setState(() {});
  }

  Future<String?> _findTailscaleIp() async {
    for (final interface in await NetworkInterface.list(
      type: InternetAddressType.IPv4,
    )) {
      for (final address in interface.addresses) {
        if (address.address.startsWith('100.')) return address.address;
      }
    }
    return null;
  }

  Future<void> _refresh() async {
    if (!listUnlocked) {
      files = [];
      if (mounted) setState(() {});
      return;
    }
    files = await server?.files() ?? [];
    if (mounted) setState(() {});
  }

  String _passwordHash(String password, String salt) {
    return sha256.convert(utf8.encode('$salt:$password')).toString();
  }

  bool _passwordMatches(String password) {
    if (!listPasswordConfigured) return false;
    return _passwordHash(password, listPasswordSalt!) == listPasswordHash;
  }

  String _newSalt() {
    final random = Random.secure();
    return base64UrlEncode(List<int>.generate(24, (_) => random.nextInt(256)));
  }

  Future<void> _configureListPassword() async {
    final currentController = TextEditingController();
    final newController = TextEditingController();
    final confirmController = TextEditingController();
    String? dialogError;
    final password = await showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(listPasswordConfigured ? '보호 비밀번호 변경' : '보호 비밀번호 설정'),
          content: SizedBox(
            width: 380,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (listPasswordConfigured) ...[
                  TextField(
                    controller: currentController,
                    autofocus: true,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: '현재 비밀번호'),
                  ),
                  const SizedBox(height: 12),
                ],
                TextField(
                  controller: newController,
                  autofocus: !listPasswordConfigured,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: '새 비밀번호 (4자 이상)',
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: confirmController,
                  obscureText: true,
                  onSubmitted: (_) {},
                  decoration: const InputDecoration(labelText: '새 비밀번호 확인'),
                ),
                if (dialogError != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(
                      dialogError!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('취소'),
            ),
            FilledButton(
              onPressed: () {
                if (listPasswordConfigured &&
                    !_passwordMatches(currentController.text)) {
                  setDialogState(() => dialogError = '현재 비밀번호가 일치하지 않습니다.');
                  return;
                }
                if (newController.text.length < 4) {
                  setDialogState(() => dialogError = '비밀번호를 4자 이상 입력하세요.');
                  return;
                }
                if (newController.text != confirmController.text) {
                  setDialogState(() => dialogError = '새 비밀번호가 서로 일치하지 않습니다.');
                  return;
                }
                Navigator.pop(context, newController.text);
              },
              child: const Text('저장'),
            ),
          ],
        ),
      ),
    );
    currentController.dispose();
    newController.dispose();
    confirmController.dispose();
    if (password == null) return;
    final salt = _newSalt();
    final hash = _passwordHash(password, salt);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('list_password_salt', salt);
    await prefs.setString('list_password_hash', hash);
    listPasswordSalt = salt;
    listPasswordHash = hash;
    listUnlocked = true;
    await _refresh();
  }

  Future<void> _unlockList() async {
    if (!listPasswordConfigured) {
      await _configureListPassword();
      return;
    }
    final controller = TextEditingController();
    String? dialogError;
    final unlocked = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('파일 및 폴더 잠금 해제'),
          content: SizedBox(
            width: 360,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: controller,
                  autofocus: true,
                  obscureText: true,
                  onSubmitted: (_) {
                    if (_passwordMatches(controller.text)) {
                      Navigator.pop(context, true);
                    } else {
                      setDialogState(() => dialogError = '비밀번호가 일치하지 않습니다.');
                    }
                  },
                  decoration: const InputDecoration(labelText: '보호 비밀번호'),
                ),
                if (dialogError != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(
                      dialogError!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('취소'),
            ),
            FilledButton(
              onPressed: () {
                if (_passwordMatches(controller.text)) {
                  Navigator.pop(context, true);
                } else {
                  setDialogState(() => dialogError = '비밀번호가 일치하지 않습니다.');
                }
              },
              child: const Text('잠금 해제'),
            ),
          ],
        ),
      ),
    );
    controller.dispose();
    if (unlocked != true) return;
    listUnlocked = true;
    await _refresh();
  }

  void _lockList() {
    listUnlocked = false;
    files = [];
    setState(() {});
  }

  Future<void> _addMediaFiles() async {
    if (!listUnlocked) {
      await _unlockList();
      if (!listUnlocked) return;
    }
    final selection = await FilePicker.pickFiles(type: FileType.media);
    if (selection.isEmpty || server == null) return;
    try {
      final added = await server!.addFiles(selection);
      await _refresh();
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('사진·동영상 $added개를 공유 폴더에 추가했습니다.')));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('파일을 추가하지 못했습니다: $e')));
    }
  }

  Widget _desktopThumbnail(SharedFile file) {
    return SizedBox(
      width: 64,
      height: 64,
      child: FutureBuilder<File?>(
        future: server?.thumbnailFor(file),
        builder: (context, snapshot) {
          final thumbnail = snapshot.data;
          if (thumbnail == null) {
            return DecoratedBox(
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(fileIcon(file.name)),
            );
          }
          return ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: Stack(
              fit: StackFit.expand,
              children: [
                Image.file(
                  thumbnail,
                  fit: BoxFit.cover,
                  cacheWidth: 160,
                  errorBuilder: (_, _, _) => Icon(fileIcon(file.name)),
                ),
                if (isVideoFile(file.name))
                  const Align(
                    alignment: Alignment.bottomRight,
                    child: Padding(
                      padding: EdgeInsets.all(4),
                      child: Icon(Icons.play_circle_fill, color: Colors.white),
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }

  Future<void> _chooseFolder() async {
    if (!listUnlocked) {
      await _unlockList();
      if (!listUnlocked) return;
    }
    final chosen = await FilePicker.getDirectoryPath(
      dialogTitle: 'BridgeBox 수신 폴더 선택',
    );
    if (chosen == null) return;
    await server?.stop();
    await subscription?.cancel();
    folderPath = chosen;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('folder', chosen);
    server = DesktopServer(folder: Directory(chosen), pin: pin);
    subscription = server!.events.stream.listen((_) => _refresh());
    await server!.start();
    await _refresh();
  }

  Future<void> _openFolder() async {
    if (!listUnlocked) {
      await _unlockList();
      if (!listUnlocked) return;
    }
    try {
      await Directory(folderPath).create(recursive: true);
      await Process.start('explorer.exe', [folderPath]);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('폴더를 열지 못했습니다: $e')));
    }
  }

  Future<void> _changePin() async {
    final controller = TextEditingController(text: pin);
    final result = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('6자리 PIN 변경'),
        content: TextField(
          controller: controller,
          autofocus: true,
          keyboardType: TextInputType.number,
          maxLength: 6,
          obscureText: true,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('취소'),
          ),
          FilledButton(
            onPressed: () => RegExp(r'^\d{6}$').hasMatch(controller.text)
                ? Navigator.pop(context, controller.text)
                : null,
            child: const Text('저장'),
          ),
        ],
      ),
    );
    if (result == null) return;
    pin = result;
    server?.pin = result;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('pin', result);
    setState(() {});
  }

  @override
  void dispose() {
    subscription?.cancel();
    server?.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final ready = server?.running ?? false;
    return Scaffold(
      appBar: AppBar(
        title: const Text(appName),
        actions: [
          IconButton(onPressed: _refresh, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 920),
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            ready ? Icons.check_circle : Icons.error,
                            color: ready ? Colors.green : Colors.red,
                          ),
                          const SizedBox(width: 10),
                          Text(
                            ready ? '파일 서버 실행 중' : '파일 서버 정지됨',
                            style: Theme.of(context).textTheme.titleLarge,
                          ),
                        ],
                      ),
                      const SizedBox(height: 18),
                      const Text('휴대폰 앱에 입력할 컴퓨터 주소'),
                      SelectableText(
                        '$tailscaleIp:$serverPort',
                        style: Theme.of(context).textTheme.headlineMedium
                            ?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Theme.of(context).colorScheme.primary,
                            ),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          const Text('연결 PIN: '),
                          Text(
                            pin,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              letterSpacing: 3,
                            ),
                          ),
                          const SizedBox(width: 12),
                          OutlinedButton(
                            onPressed: _changePin,
                            child: const Text('PIN 변경'),
                          ),
                        ],
                      ),
                      if (error != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 12),
                          child: Text(
                            error!,
                            style: const TextStyle(color: Colors.red),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 18),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Row(
                    children: [
                      Icon(listUnlocked ? Icons.folder : Icons.lock, size: 34),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              '공유 및 수신 폴더',
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            if (listUnlocked)
                              SelectableText(folderPath)
                            else
                              const Text('비밀번호를 입력해야 경로를 볼 수 있습니다.'),
                          ],
                        ),
                      ),
                      if (listUnlocked)
                        Wrap(
                          spacing: 10,
                          children: [
                            FilledButton.icon(
                              onPressed: _openFolder,
                              icon: const Icon(Icons.folder_open),
                              label: const Text('폴더 열기'),
                            ),
                            OutlinedButton(
                              onPressed: _chooseFolder,
                              child: const Text('폴더 변경'),
                            ),
                          ],
                        )
                      else
                        FilledButton.icon(
                          onPressed: _unlockList,
                          icon: Icon(
                            listPasswordConfigured
                                ? Icons.lock_open
                                : Icons.password,
                          ),
                          label: Text(
                            listPasswordConfigured ? '비밀번호 입력' : '비밀번호 설정',
                          ),
                        ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 22),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    listUnlocked ? '파일 ${files.length}개' : '컴퓨터 파일 및 폴더 잠김',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  Wrap(
                    spacing: 10,
                    children: [
                      if (listUnlocked) ...[
                        FilledButton.icon(
                          onPressed: _addMediaFiles,
                          icon: const Icon(Icons.add_photo_alternate),
                          label: const Text('사진·동영상 여러 개 추가'),
                        ),
                        FilledButton.tonalIcon(
                          onPressed: _refresh,
                          icon: const Icon(Icons.refresh),
                          label: const Text('목록 최신화'),
                        ),
                        OutlinedButton.icon(
                          onPressed: _lockList,
                          icon: const Icon(Icons.lock),
                          label: const Text('파일·폴더 잠그기'),
                        ),
                      ] else
                        FilledButton.icon(
                          onPressed: _unlockList,
                          icon: Icon(
                            listPasswordConfigured
                                ? Icons.lock_open
                                : Icons.password,
                          ),
                          label: Text(
                            listPasswordConfigured ? '비밀번호 입력' : '비밀번호 설정',
                          ),
                        ),
                      if (listPasswordConfigured)
                        TextButton(
                          onPressed: _configureListPassword,
                          child: const Text('비밀번호 변경'),
                        ),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 8),
              if (!listUnlocked)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      children: [
                        const Icon(Icons.lock, size: 46),
                        const SizedBox(height: 12),
                        Text(
                          listPasswordConfigured
                              ? '비밀번호를 입력해야 파일 목록과 폴더 경로를 볼 수 있습니다.'
                              : '먼저 파일 및 폴더 보호 비밀번호를 설정하세요.',
                        ),
                      ],
                    ),
                  ),
                ),
              if (listUnlocked && files.isEmpty)
                const Card(
                  child: Padding(
                    padding: EdgeInsets.all(30),
                    child: Center(child: Text('공유 폴더가 비어 있습니다.')),
                  ),
                ),
              if (listUnlocked)
                ...files.map(
                  (file) => Card(
                    child: ListTile(
                      leading: _desktopThumbnail(file),
                      title: Text(
                        file.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      subtitle: Text(readableSize(file.size)),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class MobileHome extends StatefulWidget {
  const MobileHome({super.key});
  @override
  State<MobileHome> createState() => _MobileHomeState();
}

class _MobileHomeState extends State<MobileHome> {
  final addressController = TextEditingController();
  final pinController = TextEditingController();
  List<SharedFile> files = [];
  bool connected = false;
  bool busy = false;
  double progress = 0;
  String status = '컴퓨터 앱에 표시된 주소와 PIN을 입력하세요.';

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    addressController.text = prefs.getString('address') ?? '';
    pinController.text = prefs.getString('pin') ?? '';
    if (mounted) setState(() {});
  }

  Uri _uri(String path) {
    var address = addressController.text.trim().replaceFirst(
      RegExp(r'^https?://'),
      '',
    );
    if (!address.contains(':')) address = '$address:$serverPort';
    return Uri.parse('http://$address$path');
  }

  Map<String, String> get headers => {
    'x-bridge-pin': pinController.text.trim(),
  };

  Future<void> _connect() async {
    setState(() {
      busy = true;
      status = '연결 확인 중...';
    });
    try {
      final response = await http
          .get(_uri('/health'), headers: headers)
          .timeout(const Duration(seconds: 8));
      if (response.statusCode == 401) throw Exception('PIN이 일치하지 않습니다.');
      if (response.statusCode != 200) throw Exception('컴퓨터 앱에 연결할 수 없습니다.');
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('address', addressController.text.trim());
      await prefs.setString('pin', pinController.text.trim());
      connected = true;
      status = '컴퓨터와 연결됐습니다.';
      await _refresh();
    } catch (e) {
      connected = false;
      status = e.toString().replaceFirst('Exception: ', '');
    } finally {
      busy = false;
      if (mounted) setState(() {});
    }
  }

  Future<void> _refresh() async {
    try {
      final response = await http
          .get(_uri('/files'), headers: headers)
          .timeout(const Duration(seconds: 10));
      if (response.statusCode != 200) throw Exception('파일 목록을 불러오지 못했습니다.');
      files = (jsonDecode(utf8.decode(response.bodyBytes)) as List)
          .map((item) => SharedFile.fromJson(item as Map<String, dynamic>))
          .toList();
      if (mounted) setState(() {});
    } catch (e) {
      status = e.toString().replaceFirst('Exception: ', '');
      if (mounted) setState(() {});
    }
  }

  Widget _mobileThumbnail(SharedFile file) {
    final thumbnailUri = _uri(
      '/thumbnail/${Uri.encodeComponent(file.name)}?v=${file.modified.millisecondsSinceEpoch}',
    );
    return SizedBox(
      width: 56,
      height: 56,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(10),
        child: Stack(
          fit: StackFit.expand,
          children: [
            Image.network(
              thumbnailUri.toString(),
              headers: headers,
              fit: BoxFit.cover,
              cacheWidth: 160,
              errorBuilder: (_, _, _) => DecoratedBox(
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                ),
                child: Icon(fileIcon(file.name)),
              ),
            ),
            if (isVideoFile(file.name))
              const Align(
                alignment: Alignment.bottomRight,
                child: Padding(
                  padding: EdgeInsets.all(3),
                  child: Icon(
                    Icons.play_circle_fill,
                    color: Colors.white,
                    size: 22,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _sendFiles() async {
    final selection = await FilePicker.pickFiles(type: FileType.media);
    if (selection.isEmpty) return;
    setState(() {
      busy = true;
      progress = 0;
      status = '사진·동영상 ${selection.length}개를 선택했습니다.';
    });
    try {
      for (var i = 0; i < selection.length; i++) {
        final picked = selection[i];
        final length = await picked.length();
        final request = http.StreamedRequest('POST', _uri('/upload'));
        request.headers.addAll(headers);
        request.headers['x-file-name'] = Uri.encodeComponent(picked.name);
        request.contentLength = length;
        final client = http.Client();
        final responseFuture = client.send(request);
        var sent = 0;
        final speedWatch = Stopwatch()..start();
        var lastUiUpdate = 0;
        try {
          await for (final chunk in picked.readAsByteStream()) {
            request.sink.add(chunk);
            sent += chunk.length;
            final now = speedWatch.elapsedMilliseconds;
            if (mounted && (now - lastUiUpdate >= 100 || sent == length)) {
              lastUiUpdate = now;
              setState(() {
                final fileProgress = length == 0 ? 0.0 : sent / length;
                progress = (i + fileProgress) / selection.length;
                final seconds = speedWatch.elapsedMilliseconds / 1000;
                final speed = seconds == 0 ? 0.0 : sent / 1048576 / seconds;
                status =
                    '${i + 1}/${selection.length} · ${picked.name} 전송 중 · ${speed.toStringAsFixed(1)} MB/s';
              });
            }
          }
          await request.sink.close();
          final response = await responseFuture;
          if (response.statusCode != 200) {
            throw Exception('${picked.name} 전송 실패 (${response.statusCode})');
          }
        } finally {
          client.close();
        }
      }
      status = '${selection.length}개 파일 전송 완료';
      await _refresh();
    } catch (e) {
      status = e.toString().replaceFirst('Exception: ', '');
    } finally {
      busy = false;
      progress = 0;
      if (mounted) setState(() {});
    }
  }

  Future<void> _download(SharedFile item) async {
    setState(() {
      busy = true;
      progress = 0;
      status = '${item.name} 다운로드 중';
    });
    File? temp;
    try {
      final request = http.Request(
        'GET',
        _uri('/file/${Uri.encodeComponent(item.name)}'),
      )..headers.addAll(headers);
      final response = await request.send();
      if (response.statusCode != 200) throw Exception('다운로드 실패');
      final directory = await getTemporaryDirectory();
      temp = File(
        '${directory.path}${Platform.pathSeparator}${cleanName(item.name)}',
      );
      final sink = temp.openWrite();
      var received = 0;
      final speedWatch = Stopwatch()..start();
      var lastUiUpdate = 0;
      await for (final chunk in response.stream) {
        sink.add(chunk);
        received += chunk.length;
        final now = speedWatch.elapsedMilliseconds;
        if (mounted && (now - lastUiUpdate >= 100 || received == item.size)) {
          lastUiUpdate = now;
          setState(() {
            progress = item.size == 0 ? 0 : received / item.size;
            final seconds = speedWatch.elapsedMilliseconds / 1000;
            final speed = seconds == 0 ? 0.0 : received / 1048576 / seconds;
            status = '${item.name} 다운로드 중 · ${speed.toStringAsFixed(1)} MB/s';
          });
        }
      }
      await sink.close();
      final video = RegExp(
        r'\.(mp4|mov|avi|mkv|webm)$',
        caseSensitive: false,
      ).hasMatch(item.name);
      final image = RegExp(
        r'\.(jpg|jpeg|png|gif|webp|heic)$',
        caseSensitive: false,
      ).hasMatch(item.name);
      if (video || image) {
        if (!await Gal.hasAccess()) await Gal.requestAccess();
        if (video) {
          await Gal.putVideo(temp.path, album: appName);
        } else {
          await Gal.putImage(temp.path, album: appName);
        }
        status = '갤러리의 $appName 앨범에 저장했습니다.';
      } else {
        final docs = await getApplicationDocumentsDirectory();
        await temp.copy(
          '${docs.path}${Platform.pathSeparator}${cleanName(item.name)}',
        );
        status = '앱 저장 공간에 저장했습니다.';
      }
    } catch (e) {
      status = e.toString().replaceFirst('Exception: ', '');
    } finally {
      if (temp != null && await temp.exists()) await temp.delete();
      busy = false;
      progress = 0;
      if (mounted) setState(() {});
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text(appName),
      actions: [
        IconButton(
          onPressed: () => Navigator.of(
            context,
          ).push(MaterialPageRoute<void>(builder: (_) => const BrowserPage())),
          icon: const Icon(Icons.language),
          tooltip: '내장 웹브라우저',
        ),
      ],
    ),
    body: SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(18),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(
                children: [
                  TextField(
                    controller: addressController,
                    enabled: !busy,
                    keyboardType: TextInputType.url,
                    decoration: const InputDecoration(
                      labelText: '컴퓨터 주소',
                      hintText: '100.127.212.114:49832',
                      prefixIcon: Icon(Icons.computer),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: pinController,
                    enabled: !busy,
                    keyboardType: TextInputType.number,
                    maxLength: 6,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: '6자리 PIN',
                      prefixIcon: Icon(Icons.lock),
                      counterText: '',
                    ),
                  ),
                  const SizedBox(height: 14),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton.icon(
                      onPressed: busy ? null : _connect,
                      icon: Icon(connected ? Icons.refresh : Icons.link),
                      label: Text(connected ? '다시 연결' : '연결'),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Icon(
                        connected ? Icons.check_circle : Icons.info_outline,
                        color: connected ? Colors.green : null,
                      ),
                      const SizedBox(width: 8),
                      Expanded(child: Text(status)),
                    ],
                  ),
                  if (busy)
                    Padding(
                      padding: const EdgeInsets.only(top: 14),
                      child: LinearProgressIndicator(
                        value: progress == 0 ? null : progress,
                      ),
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 14),
          SizedBox(
            height: 58,
            child: FilledButton.icon(
              onPressed: connected && !busy ? _sendFiles : null,
              icon: const Icon(Icons.upload),
              label: const Text('사진·동영상 여러 개 선택해서 보내기'),
            ),
          ),
          const SizedBox(height: 22),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('컴퓨터 파일', style: Theme.of(context).textTheme.titleLarge),
              IconButton(
                onPressed: connected && !busy ? _refresh : null,
                icon: const Icon(Icons.refresh),
              ),
            ],
          ),
          if (connected && files.isEmpty)
            const Card(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Center(child: Text('컴퓨터 공유 폴더가 비어 있습니다.')),
              ),
            ),
          ...files.map(
            (file) => Card(
              child: ListTile(
                leading: _mobileThumbnail(file),
                title: Text(
                  file.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                subtitle: Text(readableSize(file.size)),
                trailing: IconButton(
                  onPressed: busy ? null : () => _download(file),
                  icon: const Icon(Icons.download),
                  tooltip: '휴대폰에 저장',
                ),
              ),
            ),
          ),
        ],
      ),
    ),
  );
}

class BrowserPage extends StatefulWidget {
  const BrowserPage({super.key});

  @override
  State<BrowserPage> createState() => _BrowserPageState();
}

class _BrowserPageState extends State<BrowserPage> {
  late final WebViewController webController;
  final addressController = TextEditingController();
  int loadingProgress = 0;
  String? pageError;
  String dnsStatus = 'DNS 확인 중';
  String publicIp = 'IP 확인 중';
  String countryLocation = '위치 확인 중';
  bool networkInfoLoading = false;

  @override
  void initState() {
    super.initState();
    webController = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setBackgroundColor(Colors.white)
      ..setNavigationDelegate(
        NavigationDelegate(
          onProgress: (progress) {
            if (mounted) setState(() => loadingProgress = progress);
          },
          onPageStarted: (url) {
            addressController.text = url;
            if (mounted) setState(() => pageError = null);
          },
          onPageFinished: (url) {
            addressController.text = url;
            if (mounted) setState(() => loadingProgress = 100);
          },
          onWebResourceError: (error) {
            if (error.isForMainFrame == false) return;
            if (mounted) setState(() => pageError = error.description);
          },
        ),
      )
      ..loadRequest(Uri.parse('https://duckduckgo.com'));
    _loadNetworkInfo();
  }

  Future<void> _loadNetworkInfo() async {
    if (networkInfoLoading) return;
    setState(() => networkInfoLoading = true);
    var nextDnsStatus = '시스템 DNS';
    var nextIp = 'IP 확인 실패';
    var nextLocation = '국가 위치 확인 실패';
    try {
      final addresses = await InternetAddress.lookup('desktop-gq3uqp0')
          .timeout(const Duration(seconds: 5));
      if (addresses.any((address) => address.address.startsWith('100.'))) {
        nextDnsStatus = 'Tailscale DNS 사용 중';
      }
    } catch (_) {}
    try {
      final response = await http
          .get(Uri.parse('https://ipwho.is/'))
          .timeout(const Duration(seconds: 8));
      if (response.statusCode == 200) {
        final data = jsonDecode(utf8.decode(response.bodyBytes));
        if (data is Map<String, dynamic> && data['success'] == true) {
          nextIp = data['ip']?.toString() ?? nextIp;
          final flag = data['flag'] is Map
              ? (data['flag'] as Map)['emoji']?.toString() ?? ''
              : '';
          final country = data['country']?.toString() ?? '';
          final city = data['city']?.toString() ?? '';
          nextLocation = [
            if (flag.isNotEmpty) flag,
            if (country.isNotEmpty) country,
            if (city.isNotEmpty) city,
          ].join(' · ');
        }
      }
    } catch (_) {}
    if (!mounted) return;
    setState(() {
      dnsStatus = nextDnsStatus;
      publicIp = nextIp;
      countryLocation = nextLocation;
      networkInfoLoading = false;
    });
  }

  Uri _addressToUri(String input) {
    final value = input.trim();
    final parsed = Uri.tryParse(value);
    if (parsed != null &&
        (parsed.scheme == 'http' || parsed.scheme == 'https')) {
      return parsed;
    }
    if (!value.contains(' ') && value.contains('.')) {
      return Uri.parse('https://$value');
    }
    return Uri.https('duckduckgo.com', '/', {'q': value});
  }

  Future<void> _navigate() async {
    final value = addressController.text.trim();
    if (value.isEmpty) return;
    FocusManager.instance.primaryFocus?.unfocus();
    await webController.loadRequest(_addressToUri(value));
  }

  @override
  void dispose() {
    addressController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        titleSpacing: 0,
        title: TextField(
          controller: addressController,
          keyboardType: TextInputType.url,
          textInputAction: TextInputAction.go,
          onSubmitted: (_) => _navigate(),
          decoration: InputDecoration(
            hintText: '주소 입력 또는 검색',
            isDense: true,
            filled: true,
            fillColor: Theme.of(context).colorScheme.surfaceContainerHighest,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(24),
              borderSide: BorderSide.none,
            ),
            suffixIcon: IconButton(
              onPressed: _navigate,
              icon: const Icon(Icons.arrow_forward),
              tooltip: '이동',
            ),
          ),
        ),
        actions: [
          IconButton(
            onPressed: () => webController.reload(),
            icon: const Icon(Icons.refresh),
            tooltip: '새로고침',
          ),
        ],
      ),
      body: Column(
        children: [
          if (loadingProgress < 100)
            LinearProgressIndicator(value: loadingProgress / 100),
          Material(
            color: Theme.of(context).colorScheme.surfaceContainerLow,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              child: Row(
                children: [
                  Icon(
                    dnsStatus.startsWith('Tailscale')
                        ? Icons.verified_user
                        : Icons.dns,
                    size: 18,
                    color: dnsStatus.startsWith('Tailscale')
                        ? Colors.green
                        : null,
                  ),
                  const SizedBox(width: 7),
                  Expanded(
                    child: Text(
                      '$dnsStatus  ·  $publicIp  ·  $countryLocation',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ),
                  IconButton(
                    onPressed: networkInfoLoading ? null : _loadNetworkInfo,
                    icon: const Icon(Icons.sync, size: 19),
                    tooltip: 'IP 및 DNS 정보 새로고침',
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ),
            ),
          ),
          if (pageError != null)
            MaterialBanner(
              content: Text('페이지를 불러오지 못했습니다: $pageError'),
              actions: [
                TextButton(
                  onPressed: () {
                    setState(() => pageError = null);
                    webController.reload();
                  },
                  child: const Text('다시 시도'),
                ),
              ],
            ),
          Expanded(child: WebViewWidget(controller: webController)),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: [
            IconButton(
              onPressed: () async {
                if (await webController.canGoBack()) {
                  await webController.goBack();
                }
              },
              icon: const Icon(Icons.arrow_back),
              tooltip: '뒤로',
            ),
            IconButton(
              onPressed: () => webController.loadRequest(
                Uri.parse('https://duckduckgo.com'),
              ),
              icon: const Icon(Icons.home),
              tooltip: '홈',
            ),
            IconButton(
              onPressed: () async {
                if (await webController.canGoForward()) {
                  await webController.goForward();
                }
              },
              icon: const Icon(Icons.arrow_forward),
              tooltip: '앞으로',
            ),
          ],
        ),
      ),
    );
  }
}
