#include "main_window.h"

#include <QApplication>
#include <QScrollArea>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QMessageBox>
#include <QInputDialog>
#include <QThreadPool>

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

        QString mode = obj["audio_mode"].toString().toLower();
        m_btnModeHQ->setChecked(mode.contains("high") || mode.contains("one"));
        m_btnModeGaming->setChecked(mode.contains("gaming"));
        m_btnModeBroadcast->setChecked(mode.contains("broadcast"));

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
    } else {
        m_statusBadge->setText("Dongle Missing");
        m_statusBadge->setStyleSheet("padding: 6px 14px; border-radius: 12px; font-size: 12px; font-weight: bold; background: rgba(239, 68, 68, 0.2); color: #ef4444; border: 1px solid rgba(239, 68, 68, 0.4);");
        m_modelLabel->setText("Please plug in your Sennheiser BTD adapter");
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
    }
}

void MainWindow::onModeClicked(const QString &mode) {
    m_btnModeHQ->setChecked(mode == "one-to-one");
    m_btnModeGaming->setChecked(mode == "gaming");
    m_btnModeBroadcast->setChecked(mode == "broadcast");

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
