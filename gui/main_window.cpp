#include "main_window.h"

#include <QApplication>
#include <QScrollArea>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QMessageBox>
#include <QInputDialog>
#include <QThreadPool>
#include <QDateTime>
#include <QPainter>
#include <QPainterPath>
#include <QPixmap>
#include <QIcon>
#include <QFont>
#include <QColor>

extern "C" {
    char* btd_get_dongle_status_json();
    bool btd_set_mode(const char* mode);
    bool btd_set_codec(uint8_t bit);
    bool btd_set_broadcast(int enable, const char* name, int quality, const char* key);
    bool btd_pair();
    bool btd_reset();
    char* btd_get_headset_status_json();
    char* btd_get_headset_devices_json();
    bool btd_select_headset(const char* mac);
    bool btd_toggle_anc();
    bool btd_set_anc(bool enabled);
    bool btd_set_anc_strength(int strength);
    bool btd_set_transparency(int level);
    bool btd_set_adaptive_anc(bool enabled);
    bool btd_set_anti_wind(const char* mode);
    bool btd_set_bass_boost(bool enabled);
    void btd_free_string(char* s);
}

void StatusWorker::run() {
    while (m_running) {
        char *rawDongle = btd_get_dongle_status_json();
        if (rawDongle) {
            QString json = QString::fromUtf8(rawDongle);
            btd_free_string(rawDongle);
            emit dongleStatusReceived(json);
        }

        if (!m_running) break;

        char *rawHeadset = btd_get_headset_status_json();
        if (rawHeadset) {
            QString json = QString::fromUtf8(rawHeadset);
            btd_free_string(rawHeadset);
            emit headsetStatusReceived(json);
        }

        // Sleep 1200ms in 100ms slices for instant cancellation
        for (int i = 0; i < 12 && m_running; ++i) {
            QThread::msleep(100);
        }
    }
}

MainWindow::MainWindow(QWidget *parent) : QMainWindow(parent) {
    setWindowTitle("Sennheiser Dongle Control (Linux Qt6)");
    resize(760, 850);
    setMinimumSize(680, 720);

    setupUi();
    applyDarkTheme();
    setupTrayIcon();

    m_worker = new StatusWorker(this);
    connect(m_worker, &StatusWorker::dongleStatusReceived, this, &MainWindow::onDongleStatusReceived);
    connect(m_worker, &StatusWorker::headsetStatusReceived, this, &MainWindow::onHeadsetStatusReceived);
    m_worker->start();
}

MainWindow::~MainWindow() {
    if (m_worker) {
        m_worker->stop();
    }
}

void MainWindow::setupUi() {
    auto *centralWidget = new QWidget(this);
    setCentralWidget(centralWidget);

    auto *rootLayout = new QVBoxLayout(centralWidget);
    rootLayout->setContentsMargins(20, 20, 20, 20);
    rootLayout->setSpacing(16);

    // 1. Header Bar
    auto *headerLayout = new QHBoxLayout();
    auto *titleLayout = new QVBoxLayout();
    auto *titleLabel = new QLabel("Sennheiser Dongle Control", this);
    titleLabel->setObjectName("appTitle");
    titleLabel->setStyleSheet("font-size: 20px; font-weight: bold; color: #ffffff;");

    m_modelLabel = new QLabel("Detecting adapter...", this);
    m_modelLabel->setStyleSheet("font-size: 13px; color: #8c93a0;");
    titleLayout->addWidget(titleLabel);
    titleLayout->addWidget(m_modelLabel);

    headerLayout->addLayout(titleLayout);
    headerLayout->addStretch();

    m_statusBadge = new QLabel("Standby", this);
    m_statusBadge->setObjectName("statusBadge");
    m_statusBadge->setStyleSheet("padding: 6px 14px; border-radius: 12px; font-size: 12px; font-weight: bold; background: #232730; color: #8c93a0;");
    headerLayout->addWidget(m_statusBadge);

    rootLayout->addLayout(headerLayout);

    // 2. Scrollable Body
    auto *scrollArea = new QScrollArea(this);
    scrollArea->setWidgetResizable(true);
    scrollArea->setFrameShape(QFrame::NoFrame);
    scrollArea->setStyleSheet("background: transparent;");

    auto *scrollContent = new QWidget();
    auto *contentLayout = new QVBoxLayout(scrollContent);
    contentLayout->setContentsMargins(0, 0, 0, 0);
    contentLayout->setSpacing(16);

    // Mode Selection Card
    auto *modeGroup = new QGroupBox("Audio Link Modes", scrollContent);
    auto *modeLayout = new QHBoxLayout(modeGroup);
    modeLayout->setSpacing(12);

    m_btnModeHQ = new QPushButton("🎧 One-to-One\nHigh Quality Audio", modeGroup);
    m_btnModeHQ->setCheckable(true);
    m_btnModeHQ->setMinimumHeight(64);
    connect(m_btnModeHQ, &QPushButton::clicked, [this]() { onModeClicked("one-to-one"); });

    m_btnModeGaming = new QPushButton("🎮 Gaming Mode\nUltra Low Latency", modeGroup);
    m_btnModeGaming->setCheckable(true);
    m_btnModeGaming->setMinimumHeight(64);
    connect(m_btnModeGaming, &QPushButton::clicked, [this]() { onModeClicked("gaming"); });

    m_btnModeBroadcast = new QPushButton("📡 Auracast™\nBroadcast Stream", modeGroup);
    m_btnModeBroadcast->setCheckable(true);
    m_btnModeBroadcast->setMinimumHeight(64);
    connect(m_btnModeBroadcast, &QPushButton::clicked, [this]() { onModeClicked("broadcast"); });

    modeLayout->addWidget(m_btnModeHQ);
    modeLayout->addWidget(m_btnModeGaming);
    modeLayout->addWidget(m_btnModeBroadcast);
    contentLayout->addWidget(modeGroup);

    // Stream Details Card
    auto *streamGroup = new QGroupBox("Live Audio Stream", scrollContent);
    auto *streamLayout = new QGridLayout(streamGroup);
    streamLayout->setHorizontalSpacing(24);
    streamLayout->setVerticalSpacing(10);

    auto addStreamMetric = [&](const QString &label, QLabel* &valWidget, int row, int col) {
        auto *lbl = new QLabel(label, streamGroup);
        lbl->setStyleSheet("color: #8c93a0; font-size: 12px; font-weight: normal;");
        valWidget = new QLabel("-", streamGroup);
        valWidget->setStyleSheet("color: #ffffff; font-size: 14px; font-weight: bold;");
        streamLayout->addWidget(lbl, row, col);
        streamLayout->addWidget(valWidget, row + 1, col);
    };

    addStreamMetric("ACTIVE CODEC", m_activeCodecLabel, 0, 0);
    addStreamMetric("SAMPLE RATE", m_sampleRateLabel, 0, 1);
    addStreamMetric("BIT DEPTH", m_bitDepthLabel, 0, 2);
    addStreamMetric("TRANSPORT", m_transportLabel, 0, 3);

    contentLayout->addWidget(streamGroup);

    // Supported Codecs Switcher
    auto *codecsGroup = new QGroupBox("Available Codecs", scrollContent);
    auto *codecsMainLayout = new QVBoxLayout(codecsGroup);
    m_codecsLayout = new QHBoxLayout();
    m_codecsLayout->setSpacing(8);
    codecsMainLayout->addLayout(m_codecsLayout);
    contentLayout->addWidget(codecsGroup);

    // Sennheiser Headset Controls (HDB 630 / Momentum 4)
    auto *headsetGroup = new QGroupBox("HDB 630 / Sennheiser Headset Controls", scrollContent);
    auto *headsetLayout = new QVBoxLayout(headsetGroup);
    headsetLayout->setSpacing(14);

    // Row 1: Buttons
    auto *hsBtnLayout = new QHBoxLayout();
    m_btnAnc = new QPushButton("ANC: Checking...", headsetGroup);
    m_btnAnc->setMinimumHeight(40);
    connect(m_btnAnc, &QPushButton::clicked, this, &MainWindow::onToggleAnc);

    m_btnAdaptive = new QPushButton("Adaptive: Off", headsetGroup);
    m_btnAdaptive->setMinimumHeight(40);
    connect(m_btnAdaptive, &QPushButton::clicked, this, &MainWindow::onToggleAdaptive);

    m_btnBass = new QPushButton("Bass Boost: Off", headsetGroup);
    m_btnBass->setMinimumHeight(40);
    connect(m_btnBass, &QPushButton::clicked, this, &MainWindow::onToggleBass);

    hsBtnLayout->addWidget(m_btnAnc);
    hsBtnLayout->addWidget(m_btnAdaptive);
    hsBtnLayout->addWidget(m_btnBass);
    headsetLayout->addLayout(hsBtnLayout);

    // Row 2: Dual Synced Sliders
    auto *slidersGrid = new QGridLayout();
    auto *ancLabelLayout = new QHBoxLayout();
    ancLabelLayout->addWidget(new QLabel("ANC Strength", headsetGroup));
    m_ancStrengthVal = new QLabel("100%", headsetGroup);
    m_ancStrengthVal->setStyleSheet("color: #009fe3; font-weight: bold;");
    ancLabelLayout->addWidget(m_ancStrengthVal);
    slidersGrid->addLayout(ancLabelLayout, 0, 0);

    m_ancStrengthSlider = new QSlider(Qt::Horizontal, headsetGroup);
    m_ancStrengthSlider->setRange(0, 100);
    m_ancStrengthSlider->setValue(100);
    connect(m_ancStrengthSlider, &QSlider::sliderMoved, this, &MainWindow::onAncStrengthSliderMoved);
    connect(m_ancStrengthSlider, &QSlider::sliderReleased, this, &MainWindow::onAncStrengthReleased);
    slidersGrid->addWidget(m_ancStrengthSlider, 1, 0);

    auto *transLabelLayout = new QHBoxLayout();
    transLabelLayout->addWidget(new QLabel("Transparency Mode", headsetGroup));
    m_transVal = new QLabel("0%", headsetGroup);
    m_transVal->setStyleSheet("color: #009fe3; font-weight: bold;");
    transLabelLayout->addWidget(m_transVal);
    slidersGrid->addLayout(transLabelLayout, 0, 1);

    m_transSlider = new QSlider(Qt::Horizontal, headsetGroup);
    m_transSlider->setRange(0, 100);
    m_transSlider->setValue(0);
    connect(m_transSlider, &QSlider::sliderMoved, this, &MainWindow::onTransparencySliderMoved);
    connect(m_transSlider, &QSlider::sliderReleased, this, &MainWindow::onTransparencyReleased);
    slidersGrid->addWidget(m_transSlider, 1, 1);

    headsetLayout->addLayout(slidersGrid);

    // Row 3: Bottom info & Anti-wind
    auto *bottomHsLayout = new QHBoxLayout();
    bottomHsLayout->addWidget(new QLabel("Anti-Wind:", headsetGroup));
    m_antiWindCombo = new QComboBox(headsetGroup);
    m_antiWindCombo->addItems({"Off", "Auto", "Max"});
    connect(m_antiWindCombo, QOverload<int>::of(&QComboBox::currentIndexChanged), this, &MainWindow::onAntiWindChanged);
    bottomHsLayout->addWidget(m_antiWindCombo);

    bottomHsLayout->addSpacing(16);
    m_headsetDeviceLabel = new QLabel("Device: Auto-Detect", headsetGroup);
    m_headsetDeviceLabel->setStyleSheet("color: #8c93a0;");
    bottomHsLayout->addWidget(m_headsetDeviceLabel);
    bottomHsLayout->addStretch();

    auto *btnScan = new QPushButton("Scan / Switch Headset", headsetGroup);
    btnScan->setStyleSheet("padding: 4px 12px;");
    connect(btnScan, &QPushButton::clicked, this, &MainWindow::onScanHeadsets);
    bottomHsLayout->addWidget(btnScan);

    headsetLayout->addLayout(bottomHsLayout);
    contentLayout->addWidget(headsetGroup);

    // Auracast Broadcast Configuration
    auto *bcastGroup = new QGroupBox("Auracast™ Broadcast Settings", scrollContent);
    auto *bcastLayout = new QGridLayout(bcastGroup);

    bcastLayout->addWidget(new QLabel("Broadcast Name:", bcastGroup), 0, 0);
    m_bcastNameEdit = new QLineEdit(bcastGroup);
    m_bcastNameEdit->setPlaceholderText("Sennheiser BTD 700");
    bcastLayout->addWidget(m_bcastNameEdit, 0, 1);

    bcastLayout->addWidget(new QLabel("Audio Quality:", bcastGroup), 0, 2);
    m_bcastQualityCombo = new QComboBox(bcastGroup);
    m_bcastQualityCombo->addItem("High Quality (48 kHz)", 2);
    m_bcastQualityCombo->addItem("Standard Quality (24 kHz)", 1);
    m_bcastQualityCombo->addItem("Standard Quality (16 kHz)", 0);
    bcastLayout->addWidget(m_bcastQualityCombo, 0, 3);

    bcastLayout->addWidget(new QLabel("Encryption PIN:", bcastGroup), 1, 0);
    m_bcastKeyEdit = new QLineEdit(bcastGroup);
    m_bcastKeyEdit->setEchoMode(QLineEdit::Password);
    m_bcastKeyEdit->setPlaceholderText("Leave empty for open public broadcast");
    bcastLayout->addWidget(m_bcastKeyEdit, 1, 1);

    auto *btnSaveBcast = new QPushButton("Save Broadcast Config", bcastGroup);
    connect(btnSaveBcast, &QPushButton::clicked, this, &MainWindow::onApplyBroadcast);
    bcastLayout->addWidget(btnSaveBcast, 1, 3);

    contentLayout->addWidget(bcastGroup);

    // Dongle Actions
    auto *actionsGroup = new QGroupBox("Dongle Actions", scrollContent);
    auto *actionsLayout = new QHBoxLayout(actionsGroup);

    auto *btnPair = new QPushButton("Trigger Pairing Mode", actionsGroup);
    btnPair->setMinimumHeight(36);
    connect(btnPair, &QPushButton::clicked, this, &MainWindow::onTriggerPairing);

    auto *btnReset = new QPushButton("Factory Reset", actionsGroup);
    btnReset->setMinimumHeight(36);
    btnReset->setStyleSheet("background: rgba(239, 68, 68, 0.15); border: 1px solid rgba(239, 68, 68, 0.3); color: #ef4444;");
    connect(btnReset, &QPushButton::clicked, this, &MainWindow::onFactoryReset);

    actionsLayout->addWidget(btnPair);
    actionsLayout->addWidget(btnReset);
    contentLayout->addWidget(actionsGroup);

    scrollArea->setWidget(scrollContent);
    rootLayout->addWidget(scrollArea);
}

void MainWindow::applyDarkTheme() {
    setStyleSheet(R"(
        QMainWindow {
            background-color: #0f1115;
            color: #e4e7eb;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
        }
        QGroupBox {
            background-color: #181b22;
            border: 1px solid #282d37;
            border-radius: 10px;
            margin-top: 14px;
            padding: 18px 14px 14px 14px;
            font-weight: bold;
            font-size: 13px;
            color: #ffffff;
        }
        QGroupBox::title {
            subcontrol-origin: margin;
            subcontrol-position: top left;
            left: 14px;
            padding: 0 4px;
            color: #009fe3;
        }
        QPushButton {
            background-color: #232731;
            border: 1px solid #333a46;
            color: #ffffff;
            border-radius: 8px;
            padding: 8px 16px;
            font-weight: 600;
        }
        QPushButton:hover {
            background-color: #2b313d;
            border-color: #009fe3;
        }
        QPushButton:checked {
            background-color: rgba(0, 159, 227, 0.18);
            border-color: #009fe3;
            color: #009fe3;
        }
        QLineEdit, QComboBox {
            background-color: #232731;
            border: 1px solid #333a46;
            color: #ffffff;
            border-radius: 6px;
            padding: 6px 10px;
        }
        QLineEdit:focus, QComboBox:focus {
            border-color: #009fe3;
        }
        QSlider::groove:horizontal {
            border: none;
            height: 6px;
            background: #232731;
            border-radius: 3px;
        }
        QSlider::sub-page:horizontal {
            background: #009fe3;
            border-radius: 3px;
        }
        QSlider::handle:horizontal {
            background: #ffffff;
            border: 2px solid #009fe3;
            width: 16px;
            margin-top: -5px;
            margin-bottom: -5px;
            border-radius: 8px;
        }
        QScrollBar:vertical {
            background: #0f1115;
            width: 8px;
        }
        QScrollBar:handle:vertical {
            background: #282d37;
            border-radius: 4px;
        }
    )");
}

void MainWindow::onDongleStatusReceived(const QString &rawDongle) {
    QJsonDocument doc = QJsonDocument::fromJson(rawDongle.toUtf8());
    if (!doc.isObject()) return;
    QJsonObject obj = doc.object();

    bool connected = obj["connected"].toBool();
    if (connected) {
        QString state = obj["dongle_state"].toString("Connected");
        m_statusBadge->setText(state);
        m_statusBadge->setStyleSheet("padding: 6px 14px; border-radius: 12px; font-size: 12px; font-weight: bold; background: rgba(34, 197, 94, 0.2); color: #22c55e; border: 1px solid rgba(34, 197, 94, 0.4);");
        m_modelLabel->setText(obj["model"].toString("Sennheiser BTD 700") + " • " + obj["serial"].toString(""));

        if (QDateTime::currentMSecsSinceEpoch() - m_lastModeChangeTime > 2000) {
            QString mode = obj["audio_mode"].toString().toLower();
            m_btnModeHQ->setChecked(mode.contains("high") || mode.contains("one"));
            m_btnModeGaming->setChecked(mode.contains("gaming"));
            m_btnModeBroadcast->setChecked(mode.contains("broadcast"));
        }

        m_activeCodecLabel->setText(obj["codec_in_use"].toString("-"));
        m_sampleRateLabel->setText(obj["frequency"].toString("-"));
        m_bitDepthLabel->setText(obj["resolution"].toString("-"));
        m_transportLabel->setText(obj["connected_transport"].toString(obj["transport_mode"].toString("-")));

        // Update Codecs list with diffing (prevent widget thrashing)
        QJsonArray codecs = obj["supported_codecs"].toArray();
        QString sig;
        for (const auto &cVal : codecs) {
            sig += cVal.toObject()["name"].toString() + ";";
        }

        if (sig != m_lastCodecSignature) {
            m_lastCodecSignature = sig;
            while (QLayoutItem *item = m_codecsLayout->takeAt(0)) {
                if (item->widget()) item->widget()->deleteLater();
                delete item;
            }
            m_codecButtons.clear();

            for (const auto &cVal : codecs) {
                QJsonObject cObj = cVal.toObject();
                QString cName = cObj["name"].toString();
                int bit = cObj["bit"].toInt();
                bool active = cObj["is_active"].toBool();

                auto *b = new QPushButton(cName, this);
                b->setCheckable(true);
                b->setChecked(active);
                b->setProperty("codec_bit", bit);
                connect(b, &QPushButton::clicked, [this, bit]() { onCodecClicked(bit); });
                m_codecsLayout->addWidget(b);
                m_codecButtons.append(b);
            }
        } else {
            // Just update check states
            for (int i = 0; i < codecs.size() && i < m_codecButtons.size(); ++i) {
                bool active = codecs[i].toObject()["is_active"].toBool();
                m_codecButtons[i]->setChecked(active);
            }
        }

        if (!m_bcastNameEdit->hasFocus()) {
            m_bcastNameEdit->setText(obj["broadcast_name"].toString());
        }

        QString mode = obj["audio_mode"].toString("one-to-one");
        QString model = obj["model"].toString("Sennheiser BTD 700");
        QString codec = obj["codec_in_use"].toString("-");
        updateTrayIcon(mode, true, m_lastAncActive, model, codec, m_lastHeadsetName);
    } else {
        m_statusBadge->setText("Dongle Missing");
        m_statusBadge->setStyleSheet("padding: 6px 14px; border-radius: 12px; font-size: 12px; font-weight: bold; background: rgba(239, 68, 68, 0.2); color: #ef4444; border: 1px solid rgba(239, 68, 68, 0.4);");
        m_modelLabel->setText("Please plug in your Sennheiser BTD adapter");
        updateTrayIcon("one-to-one", false, false, "BTD 700", "-", "");
    }
}

void MainWindow::onHeadsetStatusReceived(const QString &rawHeadset) {
    QJsonDocument doc = QJsonDocument::fromJson(rawHeadset.toUtf8());
    if (!doc.isObject()) return;
    QJsonObject obj = doc.object();

    bool connected = obj["connected"].toBool();
    QString mac = obj["mac"].toString();

    if (connected || !mac.isEmpty()) {
        bool anc = obj["anc_enabled"].toBool();
        QString devName = obj["device_name"].toString(obj["name"].toString(mac));
        updateTrayIcon(m_lastAudioMode, m_lastConnected, anc, m_lastModel, m_lastCodec, devName);

        m_btnAnc->setText(anc ? "ANC: ON" : "ANC: OFF");
        m_btnAnc->setStyleSheet(anc ? "background: #009fe3; color: white;" : "");

        bool adapt = obj["adaptive_anc"].toBool();
        m_btnAdaptive->setText(adapt ? "Adaptive: Auto" : "Adaptive: Off");
        m_btnAdaptive->setStyleSheet(adapt ? "background: #009fe3; color: white;" : "");

        bool bb = obj["bass_boost"].toBool();
        m_btnBass->setText(bb ? "Bass Boost: ON" : "Bass Boost: OFF");
        m_btnBass->setStyleSheet(bb ? "background: #009fe3; color: white;" : "");

        int trans = obj["transparency"].toInt(0);
        int strength = anc ? (100 - trans) : 0;

        if (!m_ancStrengthSlider->isSliderDown()) {
            m_ancStrengthSlider->blockSignals(true);
            m_ancStrengthSlider->setValue(strength);
            m_ancStrengthSlider->blockSignals(false);
            m_ancStrengthVal->setText(QString::number(strength) + "%");
        }
        if (!m_transSlider->isSliderDown()) {
            m_transSlider->blockSignals(true);
            m_transSlider->setValue(trans);
            m_transSlider->blockSignals(false);
            m_transVal->setText(QString::number(trans) + "%");
        }

        if (!m_antiWindCombo->hasFocus()) {
            QString wind = obj["anti_wind"].toString("off").toLower();
            m_antiWindCombo->blockSignals(true);
            if (wind == "max") m_antiWindCombo->setCurrentIndex(2);
            else if (wind == "auto") m_antiWindCombo->setCurrentIndex(1);
            else m_antiWindCombo->setCurrentIndex(0);
            m_antiWindCombo->blockSignals(false);
        }

        m_headsetDeviceLabel->setText("Connected: " + mac);
    } else {
        m_btnAnc->setText("ANC: Disconnected");
        m_btnAnc->setStyleSheet("");
        m_headsetDeviceLabel->setText("Pair HDB 630 via Bluetooth settings");
        updateTrayIcon(m_lastAudioMode, m_lastConnected, false, m_lastModel, m_lastCodec, "");
    }
}

void MainWindow::onModeClicked(const QString &mode) {
    m_lastModeChangeTime = QDateTime::currentMSecsSinceEpoch();
    m_btnModeHQ->setChecked(mode == "one-to-one");
    m_btnModeGaming->setChecked(mode == "gaming");
    m_btnModeBroadcast->setChecked(mode == "broadcast");

    updateTrayIcon(mode, m_lastConnected, m_lastAncActive, m_lastModel, m_lastCodec, m_lastHeadsetName);

    QThreadPool::globalInstance()->start([mode]() {
        btd_set_mode(mode.toUtf8().constData());
    });
}

void MainWindow::onCodecClicked(int bit) {
    for (auto *b : m_codecButtons) {
        b->setChecked(b->property("codec_bit").toInt() == bit);
    }

    QThreadPool::globalInstance()->start([bit]() {
        btd_set_codec(static_cast<uint8_t>(bit));
    });
}

void MainWindow::onToggleAnc() {
    bool currentOn = m_btnAnc->text().contains("ON");
    bool newOn = !currentOn;
    m_btnAnc->setText(newOn ? "ANC: ON" : "ANC: OFF");
    m_btnAnc->setStyleSheet(newOn ? "background: #009fe3; color: white;" : "");

    updateTrayIcon(m_lastAudioMode, m_lastConnected, newOn, m_lastModel, m_lastCodec, m_lastHeadsetName);

    QThreadPool::globalInstance()->start([]() {
        btd_toggle_anc();
    });
}

void MainWindow::onToggleAdaptive() {
    bool isAuto = m_btnAdaptive->text().contains("Auto");
    bool newAuto = !isAuto;
    m_btnAdaptive->setText(newAuto ? "Adaptive: Auto" : "Adaptive: Off");
    m_btnAdaptive->setStyleSheet(newAuto ? "background: #009fe3; color: white;" : "");

    QThreadPool::globalInstance()->start([newAuto]() {
        btd_set_adaptive_anc(newAuto);
    });
}

void MainWindow::onToggleBass() {
    bool isBass = m_btnBass->text().contains("ON");
    bool newBass = !isBass;
    m_btnBass->setText(newBass ? "Bass Boost: ON" : "Bass Boost: OFF");
    m_btnBass->setStyleSheet(newBass ? "background: #009fe3; color: white;" : "");

    QThreadPool::globalInstance()->start([newBass]() {
        btd_set_bass_boost(newBass);
    });
}

void MainWindow::onAncStrengthSliderMoved(int val) {
    m_ancStrengthVal->setText(QString::number(val) + "%");
    int trans = 100 - val;
    m_transVal->setText(QString::number(trans) + "%");

    m_transSlider->blockSignals(true);
    m_transSlider->setValue(trans);
    m_transSlider->blockSignals(false);
}

void MainWindow::onAncStrengthReleased() {
    int val = m_ancStrengthSlider->value();
    QThreadPool::globalInstance()->start([val]() {
        btd_set_anc_strength(val);
    });
}

void MainWindow::onTransparencySliderMoved(int val) {
    m_transVal->setText(QString::number(val) + "%");
    int strength = 100 - val;
    m_ancStrengthVal->setText(QString::number(strength) + "%");

    m_ancStrengthSlider->blockSignals(true);
    m_ancStrengthSlider->setValue(strength);
    m_ancStrengthSlider->blockSignals(false);
}

void MainWindow::onTransparencyReleased() {
    int val = m_transSlider->value();
    QThreadPool::globalInstance()->start([val]() {
        btd_set_transparency(val);
    });
}

void MainWindow::onAntiWindChanged(int index) {
    QThreadPool::globalInstance()->start([index]() {
        const char* mode = (index == 2) ? "max" : (index == 1 ? "auto" : "off");
        btd_set_anti_wind(mode);
    });
}

void MainWindow::onApplyBroadcast() {
    QString name = m_bcastNameEdit->text();
    int quality = m_bcastQualityCombo->currentData().toInt();
    QString key = m_bcastKeyEdit->text();

    QThreadPool::globalInstance()->start([name, quality, key]() {
        btd_set_broadcast(1, name.toUtf8().constData(), quality, key.toUtf8().constData());
    });
}

void MainWindow::onTriggerPairing() {
    QThreadPool::globalInstance()->start([this]() {
        if (btd_pair()) {
            QMetaObject::invokeMethod(this, [this]() {
                QMessageBox::information(this, "Pairing Mode", "Triggered Bluetooth pairing mode on dongle.");
            });
        }
    });
}

void MainWindow::onFactoryReset() {
    auto res = QMessageBox::warning(this, "Factory Reset", "Are you sure you want to restore the dongle to factory settings?", QMessageBox::Yes | QMessageBox::No);
    if (res == QMessageBox::Yes) {
        QThreadPool::globalInstance()->start([]() {
            btd_reset();
        });
    }
}

void MainWindow::onScanHeadsets() {
    QThreadPool::globalInstance()->start([this]() {
        char *raw = btd_get_headset_devices_json();
        if (!raw) return;

        QString json = QString::fromUtf8(raw);
        btd_free_string(raw);

        QMetaObject::invokeMethod(this, [this, json]() {
            QJsonDocument doc = QJsonDocument::fromJson(json.toUtf8());
            if (!doc.isArray()) return;
            QJsonArray arr = doc.array();
            if (arr.isEmpty()) {
                QMessageBox::information(this, "No Devices", "No paired Bluetooth devices found.\nPlease pair your HDB 630 in Linux Bluetooth settings.");
                return;
            }

            QStringList items;
            QStringList macs;
            for (const auto &val : arr) {
                QJsonObject o = val.toObject();
                QString mac = o["mac"].toString();
                QString name = o["name"].toString();
                items.append(name + " (" + mac + ")");
                macs.append(mac);
            }

            bool ok = false;
            QString chosen = QInputDialog::getItem(this, "Select Headset", "Choose paired Sennheiser headset:", items, 0, false, &ok);
            if (ok && !chosen.isEmpty()) {
                int idx = items.indexOf(chosen);
                if (idx >= 0 && idx < macs.size()) {
                    QString selectedMac = macs[idx];
                    QThreadPool::globalInstance()->start([selectedMac]() {
                        btd_select_headset(selectedMac.toUtf8().constData());
                    });
                }
            }
        });
    });
}

void MainWindow::setupTrayIcon() {
    if (!QSystemTrayIcon::isSystemTrayAvailable()) {
        return;
    }

    m_trayMenu = new QMenu(this);
    m_trayMenu->setStyleSheet(
        "QMenu { background-color: #1e222b; color: #f1f5f9; border: 1px solid #333a47; border-radius: 8px; padding: 6px; font-family: system-ui, sans-serif; }"
        "QMenu::item { padding: 6px 24px 6px 14px; border-radius: 4px; font-size: 13px; }"
        "QMenu::item:selected { background-color: #0284c7; color: #ffffff; }"
        "QMenu::item:disabled { color: #64748b; font-weight: bold; padding: 6px 14px; }"
        "QMenu::separator { height: 1px; background: #333a47; margin: 4px 6px; }"
    );

    m_trayStatusAction = m_trayMenu->addAction("Sennheiser BTD 700");
    m_trayStatusAction->setEnabled(false);

    m_trayMenu->addSeparator();

    // Mode actions group
    m_trayModeGroup = new QActionGroup(this);
    m_trayModeGroup->setExclusive(true);

    m_trayActionHQ = m_trayMenu->addAction("High Quality (One-to-One)");
    m_trayActionHQ->setCheckable(true);
    m_trayActionHQ->setChecked(true);
    m_trayModeGroup->addAction(m_trayActionHQ);
    connect(m_trayActionHQ, &QAction::triggered, this, [this]() {
        onModeClicked("one-to-one");
    });

    m_trayActionGaming = m_trayMenu->addAction("Gaming Mode");
    m_trayActionGaming->setCheckable(true);
    m_trayModeGroup->addAction(m_trayActionGaming);
    connect(m_trayActionGaming, &QAction::triggered, this, [this]() {
        onModeClicked("gaming");
    });

    m_trayActionBroadcast = m_trayMenu->addAction("Auracast Broadcast");
    m_trayActionBroadcast->setCheckable(true);
    m_trayModeGroup->addAction(m_trayActionBroadcast);
    connect(m_trayActionBroadcast, &QAction::triggered, this, [this]() {
        onModeClicked("broadcast");
    });

    m_trayMenu->addSeparator();

    // Headset quick control
    m_trayActionAnc = m_trayMenu->addAction("Active Noise Cancellation (ANC)");
    m_trayActionAnc->setCheckable(true);
    connect(m_trayActionAnc, &QAction::triggered, this, &MainWindow::onToggleAnc);

    m_trayMenu->addSeparator();

    m_trayActionToggleWindow = m_trayMenu->addAction("Hide Control Panel");
    connect(m_trayActionToggleWindow, &QAction::triggered, this, [this]() {
        if (isVisible() && !isMinimized()) {
            hide();
        } else {
            showNormal();
            raise();
            activateWindow();
        }
    });

    m_trayActionQuit = m_trayMenu->addAction("Quit");
    connect(m_trayActionQuit, &QAction::triggered, this, [this]() {
        m_forceQuit = true;
        qApp->quit();
    });

    connect(m_trayMenu, &QMenu::aboutToShow, this, [this]() {
        if (isVisible() && !isMinimized()) {
            m_trayActionToggleWindow->setText("Hide Control Panel");
        } else {
            m_trayActionToggleWindow->setText("Open Control Panel");
        }
    });

    m_trayIcon = new QSystemTrayIcon(this);
    m_trayIcon->setContextMenu(m_trayMenu);

    connect(m_trayIcon, &QSystemTrayIcon::activated, this, [this](QSystemTrayIcon::ActivationReason reason) {
        if (reason == QSystemTrayIcon::Trigger || reason == QSystemTrayIcon::DoubleClick) {
            if (isVisible() && !isMinimized()) {
                hide();
            } else {
                showNormal();
                raise();
                activateWindow();
            }
        }
    });

    updateTrayIcon(m_lastAudioMode, m_lastConnected, m_lastAncActive, m_lastModel, m_lastCodec, m_lastHeadsetName);
    m_trayIcon->show();
}

void MainWindow::closeEvent(QCloseEvent *event) {
    if (m_trayIcon && m_trayIcon->isVisible() && !m_forceQuit) {
        hide();
        event->ignore();
    } else {
        event->accept();
    }
}

QPixmap MainWindow::renderModePixmap(int size, const QString &mode, bool connected, bool ancActive) {
    QPixmap pix(size, size);
    pix.fill(Qt::transparent);

    QPainter p(&pix);
    p.setRenderHint(QPainter::Antialiasing, true);
    p.setRenderHint(QPainter::TextAntialiasing, true);
    p.setRenderHint(QPainter::SmoothPixmapTransform, true);

    double s = size / 64.0;
    p.scale(s, s);

    // Color definitions
    QColor bodyColor = connected ? QColor(241, 245, 249) : QColor(100, 116, 139);
    QColor shadowColor = QColor(15, 23, 42, 180);
    QColor accentColor;
    QString badgeText;
    QColor badgeBg;
    QColor badgeBorder = QColor(255, 255, 255, 220);

    QString m = mode.toLower();
    if (!connected) {
        accentColor = QColor(100, 116, 139);
        badgeText = "✕";
        badgeBg = QColor(239, 68, 68);
    } else if (m.contains("gaming")) {
        accentColor = QColor(34, 197, 94); // Vibrant Green
        badgeText = "GAME";
        badgeBg = QColor(22, 163, 74);
    } else if (m.contains("broadcast") || m.contains("auracast")) {
        accentColor = QColor(168, 85, 247); // Vibrant Purple
        badgeText = "CAST";
        badgeBg = QColor(147, 51, 234);
    } else {
        // High Quality / One-to-One
        accentColor = QColor(14, 165, 233); // Sky Blue
        badgeText = "HQ";
        badgeBg = QColor(2, 132, 199);
    }

    // 1. Draw Headphone Band
    QPainterPath band;
    band.moveTo(14, 32);
    band.cubicTo(14, 15, 50, 15, 50, 32);

    // Subtle dark outer stroke for high contrast on light backgrounds
    p.setPen(QPen(shadowColor, 5.5, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
    p.setBrush(Qt::NoBrush);
    p.drawPath(band);

    // Main band stroke
    p.setPen(QPen(bodyColor, 3.8, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
    p.drawPath(band);

    // If Auracast broadcast is active, draw radiating radio wave arcs from the top
    if (connected && (m.contains("broadcast") || m.contains("auracast"))) {
        p.setPen(QPen(accentColor, 2.5, Qt::SolidLine, Qt::RoundCap));
        // Inner arc
        p.drawArc(QRectF(22, 6, 20, 14), 45 * 16, 90 * 16);
        // Outer arc
        p.drawArc(QRectF(16, 1, 32, 20), 45 * 16, 90 * 16);
    }

    // 2. Draw Earcups
    auto drawEarcup = [&](double x, double y, double w, double h) {
        // Shadow/outline
        p.setPen(Qt::NoPen);
        p.setBrush(shadowColor);
        p.drawRoundedRect(QRectF(x - 0.75, y - 0.75, w + 1.5, h + 1.5), 4, 4);

        // Cup body
        p.setBrush(bodyColor);
        p.drawRoundedRect(QRectF(x, y, w, h), 3.5, 3.5);

        // Accent indicator bar/pill on cup
        p.setBrush(accentColor);
        p.drawRoundedRect(QRectF(x + 2, y + 4, w - 4, h - 8), 1.5, 1.5);
    };

    drawEarcup(7, 26, 11, 20);   // Left cup
    drawEarcup(46, 26, 11, 20);  // Right cup

    // 3. Draw Mode Badge in bottom-right corner
    if (!connected) {
        QRectF badgeRect(34, 34, 26, 26);
        p.setPen(QPen(badgeBorder, 2.0));
        p.setBrush(badgeBg);
        p.drawEllipse(badgeRect);

        // Draw 'X'
        p.setPen(QPen(Qt::white, 2.5, Qt::SolidLine, Qt::RoundCap));
        p.drawLine(41, 41, 53, 53);
        p.drawLine(53, 41, 41, 53);
    } else {
        bool wideBadge = (badgeText.length() > 2);
        double bw = wideBadge ? 38.0 : 30.0;
        double bh = 22.0;
        double bx = 64.0 - bw - 2.0;
        double by = 64.0 - bh - 2.0;
        QRectF badgeRect(bx, by, bw, bh);

        // Outer dark drop-shadow for badge
        p.setPen(Qt::NoPen);
        p.setBrush(shadowColor);
        p.drawRoundedRect(QRectF(bx - 0.5, by + 0.5, bw + 1.0, bh + 1.0), 6, 6);

        // Badge background
        p.setPen(QPen(badgeBorder, 1.5));
        p.setBrush(badgeBg);
        p.drawRoundedRect(badgeRect, 6, 6);

        // Badge Text
        p.setPen(Qt::white);
        QFont f = p.font();
        f.setFamily("sans-serif");
        f.setPixelSize(wideBadge ? 10 : 12);
        f.setBold(true);
        f.setWeight(QFont::Black);
        p.setFont(f);
        p.drawText(badgeRect, Qt::AlignCenter, badgeText);
    }

    // 4. If ANC is active, draw a small glowing cyan dot at top-left
    if (connected && ancActive) {
        QRectF ancDot(4, 14, 8, 8);
        p.setPen(QPen(Qt::white, 1.0));
        p.setBrush(QColor(56, 189, 248)); // Glowing cyan dot
        p.drawEllipse(ancDot);
    }

    p.end();
    return pix;
}

QIcon MainWindow::generateTrayIcon(const QString &mode, bool connected, bool ancActive) {
    QIcon icon;
    const int sizes[] = {16, 22, 24, 32, 48, 64, 128};
    for (int s : sizes) {
        icon.addPixmap(renderModePixmap(s, mode, connected, ancActive));
    }
    return icon;
}

void MainWindow::updateTrayIcon(const QString &mode, bool connected, bool ancActive,
                                const QString &model, const QString &codec, const QString &headsetName) {
    m_lastAudioMode = mode;
    m_lastConnected = connected;
    m_lastAncActive = ancActive;
    m_lastModel = model;
    m_lastCodec = codec;
    m_lastHeadsetName = headsetName;

    QIcon icon = generateTrayIcon(mode, connected, ancActive);
    setWindowIcon(icon);

    if (!m_trayIcon) return;
    m_trayIcon->setIcon(icon);

    // Update tooltip
    QString tip = "Sennheiser BTD Control\n";
    if (connected) {
        tip += QString("Model: %1\n").arg(model.isEmpty() ? "BTD 700" : model);
        QString displayMode = "High Quality (One-to-One)";
        QString m = mode.toLower();
        if (m.contains("gaming")) displayMode = "Gaming Mode";
        else if (m.contains("broadcast") || m.contains("auracast")) displayMode = "Auracast Broadcast";
        tip += QString("Mode: %1\n").arg(displayMode);

        if (!codec.isEmpty() && codec != "-") {
            tip += QString("Codec: %1\n").arg(codec);
        }
        if (!headsetName.isEmpty()) {
            tip += QString("Headset: %1 (%2)").arg(headsetName).arg(ancActive ? "ANC ON" : "ANC OFF");
        }
    } else {
        tip += "No Sennheiser BTD dongle connected";
    }
    m_trayIcon->setToolTip(tip.trimmed());

    // Update menu actions
    if (m_trayStatusAction) {
        if (connected) {
            m_trayStatusAction->setText(model.isEmpty() ? "Sennheiser BTD 700 (Connected)" : QString("%1 (Connected)").arg(model));
        } else {
            m_trayStatusAction->setText("Dongle Disconnected");
        }
    }

    if (m_trayActionHQ && m_trayActionGaming && m_trayActionBroadcast) {
        m_trayActionHQ->setEnabled(connected);
        m_trayActionGaming->setEnabled(connected);
        m_trayActionBroadcast->setEnabled(connected);

        QString m = mode.toLower();
        if (m.contains("gaming")) {
            m_trayActionGaming->setChecked(true);
        } else if (m.contains("broadcast") || m.contains("auracast")) {
            m_trayActionBroadcast->setChecked(true);
        } else {
            m_trayActionHQ->setChecked(true);
        }
    }

    if (m_trayActionAnc) {
        m_trayActionAnc->setEnabled(!headsetName.isEmpty());
        m_trayActionAnc->setChecked(ancActive);
        if (!headsetName.isEmpty()) {
            m_trayActionAnc->setText(QString("Active Noise Cancellation (%1)").arg(ancActive ? "ON" : "OFF"));
        } else {
            m_trayActionAnc->setText("Active Noise Cancellation (No Headset)");
        }
    }
}

