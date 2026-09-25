#pragma once

#include <QMainWindow>
#include <QLabel>
#include <QPushButton>
#include <QSlider>
#include <QComboBox>
#include <QLineEdit>
#include <QGroupBox>
#include <QTimer>
#include <QFrame>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QGridLayout>

class MainWindow : public QMainWindow {
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);
    ~MainWindow() override = default;

private slots:
    void refreshAll();
    void onModeClicked(const QString &mode);
    void onCodecClicked(int bit);
    void onToggleAnc();
    void onToggleAdaptive();
    void onToggleBass();
    void onAncStrengthChanged(int val);
    void onTransparencyChanged(int val);
    void onAntiWindChanged(int index);
    void onApplyBroadcast();
    void onTriggerPairing();
    void onFactoryReset();
    void onScanHeadsets();

private:
    void setupUi();
    void applyDarkTheme();

    // Dongle Status Widgets
    QLabel *m_statusBadge;
    QLabel *m_modelLabel;
    QLabel *m_serialLabel;

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

    // Timer
    QTimer *m_pollTimer;
};
