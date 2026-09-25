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
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QActionGroup>
#include <QCloseEvent>
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

    static QIcon generateTrayIcon(const QString &mode, bool connected, bool ancActive);
    static QPixmap renderModePixmap(int size, const QString &mode, bool connected, bool ancActive);

protected:
    void closeEvent(QCloseEvent *event) override;

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
    void setupTrayIcon();
    void applyDarkTheme();
    void updateTrayIcon(const QString &mode, bool connected, bool ancActive,
                        const QString &model, const QString &codec, const QString &headsetName);

    StatusWorker *m_worker{nullptr};

    // System Tray
    QSystemTrayIcon *m_trayIcon{nullptr};
    QMenu *m_trayMenu{nullptr};
    QAction *m_trayStatusAction{nullptr};
    QActionGroup *m_trayModeGroup{nullptr};
    QAction *m_trayActionHQ{nullptr};
    QAction *m_trayActionGaming{nullptr};
    QAction *m_trayActionBroadcast{nullptr};
    QAction *m_trayActionAnc{nullptr};
    QAction *m_trayActionToggleWindow{nullptr};
    QAction *m_trayActionQuit{nullptr};
    bool m_forceQuit{false};

    // Cached status for tray updates
    bool m_lastConnected{false};
    QString m_lastAudioMode{"one-to-one"};
    QString m_lastModel{"Sennheiser BTD 700"};
    QString m_lastCodec{"-"};
    QString m_lastHeadsetName{""};
    bool m_lastAncActive{false};

    // Dongle Status Widgets
    QLabel *m_statusBadge{nullptr};
    QLabel *m_modelLabel{nullptr};

    // Mode Buttons / Cards
    QPushButton *m_btnModeHQ{nullptr};
    QPushButton *m_btnModeGaming{nullptr};
    QPushButton *m_btnModeBroadcast{nullptr};

    // Stream Details
    QLabel *m_activeCodecLabel{nullptr};
    QLabel *m_sampleRateLabel{nullptr};
    QLabel *m_bitDepthLabel{nullptr};
    QLabel *m_transportLabel{nullptr};

    // Codec Badges
    QHBoxLayout *m_codecsLayout{nullptr};
    QList<QPushButton*> m_codecButtons;
    QString m_lastCodecSignature;

    // Auracast Inputs
    QLineEdit *m_bcastNameEdit{nullptr};
    QComboBox *m_bcastQualityCombo{nullptr};
    QLineEdit *m_bcastKeyEdit{nullptr};

    // Headphone Widgets
    QPushButton *m_btnAnc{nullptr};
    QPushButton *m_btnAdaptive{nullptr};
    QPushButton *m_btnBass{nullptr};
    QSlider *m_ancStrengthSlider{nullptr};
    QLabel *m_ancStrengthVal{nullptr};
    QSlider *m_transSlider{nullptr};
    QLabel *m_transVal{nullptr};
    QComboBox *m_antiWindCombo{nullptr};
    QLabel *m_headsetDeviceLabel{nullptr};
    qint64 m_lastModeChangeTime{0};
};
