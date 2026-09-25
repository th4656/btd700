#pragma once

#include <QMainWindow>
#include <QLabel>
#include <QPushButton>
#include <QSlider>
#include <QComboBox>
#include <QLineEdit>
#include <QGroupBox>
#include <QFrame>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QGridLayout>
#include <QThread>
#include <atomic>

class StatusWorker : public QThread {
    Q_OBJECT

public:
    explicit StatusWorker(QObject *parent = nullptr) : QThread(parent), m_running(true) {}
    ~StatusWorker() override {
        stop();
    }

    void stop() {
        m_running = false;
        wait();
    }

signals:
    void dongleStatusReceived(const QString &json);
    void headsetStatusReceived(const QString &json);

protected:
    void run() override;

private:
    std::atomic<bool> m_running{true};
};

class MainWindow : public QMainWindow {
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);
    ~MainWindow() override;

private slots:
    void onDongleStatusReceived(const QString &json);
    void onHeadsetStatusReceived(const QString &json);

    void onModeClicked(const QString &mode);
    void onCodecClicked(int bit);
    void onToggleAnc();
    void onToggleAdaptive();
    void onToggleBass();
    void onAncStrengthSliderMoved(int val);
    void onTransparencySliderMoved(int val);
    void onAncStrengthReleased();
    void onTransparencyReleased();
    void onAntiWindChanged(int index);
    void onApplyBroadcast();
    void onTriggerPairing();
    void onFactoryReset();
    void onScanHeadsets();

private:
    void setupUi();
    void applyDarkTheme();

    StatusWorker *m_worker;

    // Dongle Status Widgets
    QLabel *m_statusBadge;
    QLabel *m_modelLabel;

    // Mode Buttons / Cards
    QPushButton *m_btnModeHQ;
    QPushButton *m_btnModeGaming;
    QPushButton *m_btnModeBroadcast;

    // Stream Details
    QLabel *m_activeCodecLabel;
    QLabel *m_sampleRateLabel;
    QLabel *m_bitDepthLabel;
    QLabel *m_transportLabel;

    // Codec Badges
    QHBoxLayout *m_codecsLayout;
    QList<QPushButton*> m_codecButtons;
    QString m_lastCodecSignature;

    // Auracast Inputs
    QLineEdit *m_bcastNameEdit;
    QComboBox *m_bcastQualityCombo;
    QLineEdit *m_bcastKeyEdit;

    // Headphone Widgets
    QPushButton *m_btnAnc;
    QPushButton *m_btnAdaptive;
    QPushButton *m_btnBass;
    QSlider *m_ancStrengthSlider;
    QLabel *m_ancStrengthVal;
    QSlider *m_transSlider;
    QLabel *m_transVal;
    QComboBox *m_antiWindCombo;
    QLabel *m_headsetDeviceLabel;
};
