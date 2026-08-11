namespace Windrop.Domain;

public static class UiText
{
    public const string WindowTitle = "WindowTitle", History = "History", Settings = "Settings";
    public const string DeviceName = "DeviceName", SaveFolder = "SaveFolder", Browse = "Browse", TcpPort = "TcpPort", Language = "Language";
    public const string PdfHandling = "PdfHandling", VisualOutput = "VisualOutput", PdfDpi = "PdfDpi";
    public const string AutoCopy = "AutoCopy", AskApproval = "AskApproval", StartWindows = "StartWindows", SaveRestart = "SaveRestart";
    public const string Received = "Received", File = "File", Type = "Type", Size = "Size", CopyAgain = "CopyAgain", ShowFolder = "ShowFolder", Open = "Open";
    public const string NoItems = "NoItems", ItemsCount = "ItemsCount", Copied = "Copied";
    public const string InvalidSettings = "InvalidSettings", InvalidSettingsTitle = "InvalidSettingsTitle", SettingsSaved = "SettingsSaved", SaveError = "SaveError", FolderPrompt = "FolderPrompt";
    public const string OpenWindrop = "OpenWindrop", Exit = "Exit", Listening = "Listening", Ready = "Ready";
    public const string ContentReceived = "ContentReceived", ContentReceivedBody = "ContentReceivedBody", Error = "Error", StartError = "StartError";
    public const string ApprovalTitle = "ApprovalTitle", ApprovalQuestion = "ApprovalQuestion", ApprovalCountdown = "ApprovalCountdown", Decline = "Decline", Accept = "Accept";
    public const string PdfChoiceTitle = "PdfChoiceTitle", PdfSummaryVisual = "PdfSummaryVisual", PdfSummaryText = "PdfSummaryText", NoText = "NoText", TextPreview = "TextPreview";
    public const string Automatic = "Automatic", SaveImage = "SaveImage", TextOnly = "TextOnly", AutoCountdown = "AutoCountdown";
    public const string HandlingAsk = "HandlingAsk", HandlingAutomatic = "HandlingAutomatic", HandlingImage = "HandlingImage", HandlingText = "HandlingText";
}

public static class Localizer
{
    private static readonly IReadOnlyDictionary<UiLanguage, IReadOnlyDictionary<string, string>> Values =
        new Dictionary<UiLanguage, IReadOnlyDictionary<string, string>>
        {
            [UiLanguage.English] = D(
                (UiText.WindowTitle, "Archura Windrop"), (UiText.History, "History"), (UiText.Settings, "Settings"),
                (UiText.DeviceName, "Device name"), (UiText.SaveFolder, "Save folder"), (UiText.Browse, "Browse…"), (UiText.TcpPort, "TCP port"), (UiText.Language, "Language"),
                (UiText.PdfHandling, "PDF handling"), (UiText.VisualOutput, "Visual PDF output"), (UiText.PdfDpi, "PDF render DPI (600 = maximum quality)"),
                (UiText.AutoCopy, "Automatically copy received content"), (UiText.AskApproval, "Ask before accepting (auto-accept after 10 seconds)"), (UiText.StartWindows, "Start with Windows"), (UiText.SaveRestart, "Save and restart bridge"),
                (UiText.Received, "Received"), (UiText.File, "File"), (UiText.Type, "Type"), (UiText.Size, "Size"), (UiText.CopyAgain, "Copy again"), (UiText.ShowFolder, "Show in folder"), (UiText.Open, "Open"),
                (UiText.NoItems, "No received items yet."), (UiText.ItemsCount, "{0} recent item(s)"), (UiText.Copied, "Copied {0}"),
                (UiText.InvalidSettings, "Enter valid settings. Port must be 1024–65535 and PDF DPI must be 72–600."), (UiText.InvalidSettingsTitle, "Invalid settings"),
                (UiText.SettingsSaved, "Settings saved and the bridge restarted."), (UiText.SaveError, "Could not save settings"), (UiText.FolderPrompt, "Choose where incoming documents are saved"),
                (UiText.OpenWindrop, "Open Windrop"), (UiText.Exit, "Exit"), (UiText.Listening, "Listening as {0}"), (UiText.Ready, "Archura Windrop is ready"),
                (UiText.ContentReceived, "Content received"), (UiText.ContentReceivedBody, "{0} was saved and copied to the clipboard."), (UiText.Error, "Windrop error"), (UiText.StartError, "Bridge could not start: {0}"),
                (UiText.ApprovalTitle, "Incoming Windrop content"), (UiText.ApprovalQuestion, "Accept content from {0}?"), (UiText.ApprovalCountdown, "It will be accepted automatically in {0} seconds."), (UiText.Decline, "Decline"), (UiText.Accept, "Accept"),
                (UiText.PdfChoiceTitle, "Choose PDF output"), (UiText.PdfSummaryVisual, "Received PDF: {0} page(s), visual/mixed content"), (UiText.PdfSummaryText, "Received PDF: {0} page(s), text content"),
                (UiText.NoText, "No extractable text layer was found."), (UiText.TextPreview, "Text preview: {0}"), (UiText.Automatic, "Automatic"), (UiText.SaveImage, "Save as image"), (UiText.TextOnly, "Text only (.txt)"), (UiText.AutoCountdown, "Automatic will be selected in {0} seconds."),
                (UiText.HandlingAsk, "Ask every time"), (UiText.HandlingAutomatic, "Automatic"), (UiText.HandlingImage, "Always image"), (UiText.HandlingText, "Text only")),
            [UiLanguage.Turkish] = D(
                (UiText.History, "Geçmiş"), (UiText.Settings, "Ayarlar"), (UiText.DeviceName, "Cihaz adı"), (UiText.SaveFolder, "Kayıt klasörü"), (UiText.Browse, "Gözat…"), (UiText.Language, "Dil"),
                (UiText.PdfHandling, "PDF işleme"), (UiText.VisualOutput, "Görsel PDF çıktısı"), (UiText.PdfDpi, "PDF render DPI (600 = en yüksek kalite)"),
                (UiText.AutoCopy, "Alınan içeriği otomatik olarak panoya kopyala"), (UiText.AskApproval, "Kabul etmeden önce sor (10 saniye sonra otomatik kabul)"), (UiText.StartWindows, "Windows ile başlat"), (UiText.SaveRestart, "Kaydet ve köprüyü yeniden başlat"),
                (UiText.Received, "Alınma zamanı"), (UiText.File, "Dosya"), (UiText.Type, "Tür"), (UiText.Size, "Boyut"), (UiText.CopyAgain, "Tekrar kopyala"), (UiText.ShowFolder, "Klasörde göster"), (UiText.Open, "Aç"),
                (UiText.NoItems, "Henüz alınan öğe yok."), (UiText.ItemsCount, "{0} son öğe"), (UiText.Copied, "{0} kopyalandı"),
                (UiText.InvalidSettings, "Geçerli ayarlar girin. Port 1024–65535, PDF DPI 72–600 arasında olmalıdır."), (UiText.InvalidSettingsTitle, "Geçersiz ayarlar"),
                (UiText.SettingsSaved, "Ayarlar kaydedildi ve köprü yeniden başlatıldı."), (UiText.SaveError, "Ayarlar kaydedilemedi"), (UiText.FolderPrompt, "Gelen belgelerin kaydedileceği klasörü seçin"),
                (UiText.OpenWindrop, "Windrop'u aç"), (UiText.Exit, "Çıkış"), (UiText.Listening, "{0} olarak dinleniyor"), (UiText.Ready, "Archura Windrop hazır"),
                (UiText.ContentReceived, "İçerik alındı"), (UiText.ContentReceivedBody, "{0} kaydedildi ve panoya kopyalandı."), (UiText.Error, "Windrop hatası"), (UiText.StartError, "Köprü başlatılamadı: {0}"),
                (UiText.ApprovalTitle, "Gelen Windrop içeriği"), (UiText.ApprovalQuestion, "{0} kaynağından gelen içerik kabul edilsin mi?"), (UiText.ApprovalCountdown, "{0} saniye sonra otomatik kabul edilecek."), (UiText.Decline, "Reddet"), (UiText.Accept, "Kabul et"),
                (UiText.PdfChoiceTitle, "PDF çıktısını seçin"), (UiText.PdfSummaryVisual, "Alınan PDF: {0} sayfa, görsel/karma içerik"), (UiText.PdfSummaryText, "Alınan PDF: {0} sayfa, metin içeriği"),
                (UiText.NoText, "Çıkarılabilir metin katmanı bulunamadı."), (UiText.TextPreview, "Metin önizlemesi: {0}"), (UiText.Automatic, "Otomatik"), (UiText.SaveImage, "Görsel olarak kaydet"), (UiText.TextOnly, "Yalnızca metin (.txt)"), (UiText.AutoCountdown, "{0} saniye sonra Otomatik seçilecek."),
                (UiText.HandlingAsk, "Her seferinde sor"), (UiText.HandlingAutomatic, "Otomatik"), (UiText.HandlingImage, "Her zaman görsel"), (UiText.HandlingText, "Yalnızca metin")),
            [UiLanguage.German] = D(
                (UiText.History, "Verlauf"), (UiText.Settings, "Einstellungen"), (UiText.DeviceName, "Gerätename"), (UiText.SaveFolder, "Speicherordner"), (UiText.Browse, "Durchsuchen…"), (UiText.Language, "Sprache"),
                (UiText.PdfHandling, "PDF-Verarbeitung"), (UiText.VisualOutput, "Visuelles PDF-Ausgabeformat"), (UiText.PdfDpi, "PDF-Render-DPI (600 = höchste Qualität)"),
                (UiText.AutoCopy, "Empfangene Inhalte automatisch kopieren"), (UiText.AskApproval, "Vor Annahme fragen (nach 10 Sekunden automatisch)"), (UiText.StartWindows, "Mit Windows starten"), (UiText.SaveRestart, "Speichern und Bridge neu starten"),
                (UiText.Received, "Empfangen"), (UiText.File, "Datei"), (UiText.Type, "Typ"), (UiText.Size, "Größe"), (UiText.CopyAgain, "Erneut kopieren"), (UiText.ShowFolder, "Im Ordner anzeigen"), (UiText.Open, "Öffnen"),
                (UiText.NoItems, "Noch keine empfangenen Elemente."), (UiText.ItemsCount, "{0} zuletzt empfangene Elemente"), (UiText.Copied, "{0} kopiert"),
                (UiText.InvalidSettings, "Gültige Einstellungen eingeben. Port: 1024–65535, PDF-DPI: 72–600."), (UiText.InvalidSettingsTitle, "Ungültige Einstellungen"),
                (UiText.SettingsSaved, "Einstellungen gespeichert und Bridge neu gestartet."), (UiText.SaveError, "Einstellungen konnten nicht gespeichert werden"), (UiText.FolderPrompt, "Ordner für eingehende Dokumente auswählen"),
                (UiText.OpenWindrop, "Windrop öffnen"), (UiText.Exit, "Beenden"), (UiText.Listening, "Bereit als {0}"), (UiText.Ready, "Archura Windrop ist bereit"),
                (UiText.ContentReceived, "Inhalt empfangen"), (UiText.ContentReceivedBody, "{0} wurde gespeichert und in die Zwischenablage kopiert."), (UiText.Error, "Windrop-Fehler"), (UiText.StartError, "Bridge konnte nicht gestartet werden: {0}"),
                (UiText.ApprovalTitle, "Eingehender Windrop-Inhalt"), (UiText.ApprovalQuestion, "Inhalt von {0} annehmen?"), (UiText.ApprovalCountdown, "Automatische Annahme in {0} Sekunden."), (UiText.Decline, "Ablehnen"), (UiText.Accept, "Annehmen"),
                (UiText.PdfChoiceTitle, "PDF-Ausgabe auswählen"), (UiText.PdfSummaryVisual, "PDF empfangen: {0} Seite(n), visueller/gemischter Inhalt"), (UiText.PdfSummaryText, "PDF empfangen: {0} Seite(n), Textinhalt"),
                (UiText.NoText, "Keine extrahierbare Textebene gefunden."), (UiText.TextPreview, "Textvorschau: {0}"), (UiText.Automatic, "Automatisch"), (UiText.SaveImage, "Als Bild speichern"), (UiText.TextOnly, "Nur Text (.txt)"), (UiText.AutoCountdown, "Automatisch wird in {0} Sekunden gewählt."),
                (UiText.HandlingAsk, "Jedes Mal fragen"), (UiText.HandlingAutomatic, "Automatisch"), (UiText.HandlingImage, "Immer Bild"), (UiText.HandlingText, "Nur Text")),
            [UiLanguage.Spanish] = D(
                (UiText.History, "Historial"), (UiText.Settings, "Ajustes"), (UiText.DeviceName, "Nombre del dispositivo"), (UiText.SaveFolder, "Carpeta de guardado"), (UiText.Browse, "Examinar…"), (UiText.Language, "Idioma"),
                (UiText.PdfHandling, "Procesamiento de PDF"), (UiText.VisualOutput, "Formato visual del PDF"), (UiText.PdfDpi, "DPI del PDF (600 = máxima calidad)"),
                (UiText.AutoCopy, "Copiar automáticamente el contenido recibido"), (UiText.AskApproval, "Preguntar antes de aceptar (automático tras 10 segundos)"), (UiText.StartWindows, "Iniciar con Windows"), (UiText.SaveRestart, "Guardar y reiniciar el puente"),
                (UiText.Received, "Recibido"), (UiText.File, "Archivo"), (UiText.Type, "Tipo"), (UiText.Size, "Tamaño"), (UiText.CopyAgain, "Copiar de nuevo"), (UiText.ShowFolder, "Mostrar en carpeta"), (UiText.Open, "Abrir"),
                (UiText.NoItems, "Aún no hay elementos recibidos."), (UiText.ItemsCount, "{0} elemento(s) reciente(s)"), (UiText.Copied, "{0} copiado"),
                (UiText.InvalidSettings, "Introduce ajustes válidos. Puerto: 1024–65535; DPI: 72–600."), (UiText.InvalidSettingsTitle, "Ajustes no válidos"),
                (UiText.SettingsSaved, "Ajustes guardados y puente reiniciado."), (UiText.SaveError, "No se pudieron guardar los ajustes"), (UiText.FolderPrompt, "Elige dónde guardar los documentos entrantes"),
                (UiText.OpenWindrop, "Abrir Windrop"), (UiText.Exit, "Salir"), (UiText.Listening, "Escuchando como {0}"), (UiText.Ready, "Archura Windrop está listo"),
                (UiText.ContentReceived, "Contenido recibido"), (UiText.ContentReceivedBody, "{0} se guardó y copió al portapapeles."), (UiText.Error, "Error de Windrop"), (UiText.StartError, "No se pudo iniciar el puente: {0}"),
                (UiText.ApprovalTitle, "Contenido entrante de Windrop"), (UiText.ApprovalQuestion, "¿Aceptar contenido de {0}?"), (UiText.ApprovalCountdown, "Se aceptará automáticamente en {0} segundos."), (UiText.Decline, "Rechazar"), (UiText.Accept, "Aceptar"),
                (UiText.PdfChoiceTitle, "Elegir salida del PDF"), (UiText.PdfSummaryVisual, "PDF recibido: {0} página(s), contenido visual/mixto"), (UiText.PdfSummaryText, "PDF recibido: {0} página(s), contenido de texto"),
                (UiText.NoText, "No se encontró una capa de texto extraíble."), (UiText.TextPreview, "Vista previa: {0}"), (UiText.Automatic, "Automático"), (UiText.SaveImage, "Guardar como imagen"), (UiText.TextOnly, "Solo texto (.txt)"), (UiText.AutoCountdown, "Se elegirá Automático en {0} segundos."),
                (UiText.HandlingAsk, "Preguntar siempre"), (UiText.HandlingAutomatic, "Automático"), (UiText.HandlingImage, "Siempre imagen"), (UiText.HandlingText, "Solo texto")),
            [UiLanguage.Russian] = D(
                (UiText.History, "История"), (UiText.Settings, "Настройки"), (UiText.DeviceName, "Имя устройства"), (UiText.SaveFolder, "Папка сохранения"), (UiText.Browse, "Обзор…"), (UiText.Language, "Язык"),
                (UiText.PdfHandling, "Обработка PDF"), (UiText.VisualOutput, "Формат изображения PDF"), (UiText.PdfDpi, "DPI PDF (600 = максимальное качество)"),
                (UiText.AutoCopy, "Автоматически копировать полученное"), (UiText.AskApproval, "Спрашивать перед приёмом (автоматически через 10 секунд)"), (UiText.StartWindows, "Запускать с Windows"), (UiText.SaveRestart, "Сохранить и перезапустить мост"),
                (UiText.Received, "Получено"), (UiText.File, "Файл"), (UiText.Type, "Тип"), (UiText.Size, "Размер"), (UiText.CopyAgain, "Копировать снова"), (UiText.ShowFolder, "Показать в папке"), (UiText.Open, "Открыть"),
                (UiText.NoItems, "Полученных элементов пока нет."), (UiText.ItemsCount, "Недавних элементов: {0}"), (UiText.Copied, "Скопировано: {0}"),
                (UiText.InvalidSettings, "Введите корректные настройки. Порт: 1024–65535; DPI: 72–600."), (UiText.InvalidSettingsTitle, "Некорректные настройки"),
                (UiText.SettingsSaved, "Настройки сохранены, мост перезапущен."), (UiText.SaveError, "Не удалось сохранить настройки"), (UiText.FolderPrompt, "Выберите папку для входящих документов"),
                (UiText.OpenWindrop, "Открыть Windrop"), (UiText.Exit, "Выход"), (UiText.Listening, "Устройство: {0}"), (UiText.Ready, "Archura Windrop готов"),
                (UiText.ContentReceived, "Содержимое получено"), (UiText.ContentReceivedBody, "{0} сохранён и скопирован в буфер обмена."), (UiText.Error, "Ошибка Windrop"), (UiText.StartError, "Не удалось запустить мост: {0}"),
                (UiText.ApprovalTitle, "Входящее содержимое Windrop"), (UiText.ApprovalQuestion, "Принять содержимое от {0}?"), (UiText.ApprovalCountdown, "Автоматический приём через {0} секунд."), (UiText.Decline, "Отклонить"), (UiText.Accept, "Принять"),
                (UiText.PdfChoiceTitle, "Выберите формат PDF"), (UiText.PdfSummaryVisual, "Получен PDF: {0} стр., визуальное/смешанное содержимое"), (UiText.PdfSummaryText, "Получен PDF: {0} стр., текст"),
                (UiText.NoText, "Извлекаемый текстовый слой не найден."), (UiText.TextPreview, "Предпросмотр текста: {0}"), (UiText.Automatic, "Автоматически"), (UiText.SaveImage, "Сохранить как изображение"), (UiText.TextOnly, "Только текст (.txt)"), (UiText.AutoCountdown, "Автоматический режим через {0} секунд."),
                (UiText.HandlingAsk, "Спрашивать каждый раз"), (UiText.HandlingAutomatic, "Автоматически"), (UiText.HandlingImage, "Всегда изображение"), (UiText.HandlingText, "Только текст")),
            [UiLanguage.SimplifiedChinese] = D(
                (UiText.History, "历史记录"), (UiText.Settings, "设置"), (UiText.DeviceName, "设备名称"), (UiText.SaveFolder, "保存文件夹"), (UiText.Browse, "浏览…"), (UiText.Language, "语言"),
                (UiText.PdfHandling, "PDF 处理方式"), (UiText.VisualOutput, "PDF 图像格式"), (UiText.PdfDpi, "PDF 渲染 DPI（600 = 最高质量）"),
                (UiText.AutoCopy, "自动复制收到的内容"), (UiText.AskApproval, "接收前询问（10 秒后自动接收）"), (UiText.StartWindows, "随 Windows 启动"), (UiText.SaveRestart, "保存并重启桥接"),
                (UiText.Received, "接收时间"), (UiText.File, "文件"), (UiText.Type, "类型"), (UiText.Size, "大小"), (UiText.CopyAgain, "再次复制"), (UiText.ShowFolder, "在文件夹中显示"), (UiText.Open, "打开"),
                (UiText.NoItems, "尚未收到任何项目。"), (UiText.ItemsCount, "最近项目：{0}"), (UiText.Copied, "已复制 {0}"),
                (UiText.InvalidSettings, "请输入有效设置。端口范围为 1024–65535，PDF DPI 范围为 72–600。"), (UiText.InvalidSettingsTitle, "设置无效"),
                (UiText.SettingsSaved, "设置已保存，桥接已重启。"), (UiText.SaveError, "无法保存设置"), (UiText.FolderPrompt, "选择传入文档的保存位置"),
                (UiText.OpenWindrop, "打开 Windrop"), (UiText.Exit, "退出"), (UiText.Listening, "正在以 {0} 监听"), (UiText.Ready, "Archura Windrop 已就绪"),
                (UiText.ContentReceived, "已收到内容"), (UiText.ContentReceivedBody, "{0} 已保存并复制到剪贴板。"), (UiText.Error, "Windrop 错误"), (UiText.StartError, "无法启动桥接：{0}"),
                (UiText.ApprovalTitle, "Windrop 传入内容"), (UiText.ApprovalQuestion, "是否接收来自 {0} 的内容？"), (UiText.ApprovalCountdown, "将在 {0} 秒后自动接收。"), (UiText.Decline, "拒绝"), (UiText.Accept, "接收"),
                (UiText.PdfChoiceTitle, "选择 PDF 输出"), (UiText.PdfSummaryVisual, "收到 PDF：{0} 页，图像/混合内容"), (UiText.PdfSummaryText, "收到 PDF：{0} 页，文本内容"),
                (UiText.NoText, "未找到可提取的文本层。"), (UiText.TextPreview, "文本预览：{0}"), (UiText.Automatic, "自动"), (UiText.SaveImage, "保存为图像"), (UiText.TextOnly, "仅文本 (.txt)"), (UiText.AutoCountdown, "将在 {0} 秒后选择自动模式。"),
                (UiText.HandlingAsk, "每次询问"), (UiText.HandlingAutomatic, "自动"), (UiText.HandlingImage, "始终保存图像"), (UiText.HandlingText, "仅文本"))
        };

    public static string Get(UiLanguage language, string key)
    {
        if (Values.TryGetValue(language, out var localized) && localized.TryGetValue(key, out var value)) return value;
        return Values[UiLanguage.English].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(UiLanguage language, string key, params object[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, Get(language, key), args);

    public static string HandlingName(UiLanguage language, PdfHandlingMode mode) => Get(language, mode switch
    {
        PdfHandlingMode.AskEveryTime => UiText.HandlingAsk,
        PdfHandlingMode.Automatic => UiText.HandlingAutomatic,
        PdfHandlingMode.Image => UiText.HandlingImage,
        _ => UiText.HandlingText
    });

    private static IReadOnlyDictionary<string, string> D(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
}

public sealed record DisplayOption<T>(T Value, string Label);
