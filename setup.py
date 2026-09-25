from setuptools import setup, find_packages

setup(
    name="btd700",
    version="1.0.5",
    description="Sennheiser Dongle Control (BTD 600 / BTD 700) for Linux",
    author="Antigravity",
    packages=find_packages(),
    package_data={
        "btd700": ["assets/*"],
    },
    include_package_data=True,
    python_requires=">=3.8",
    entry_points={
        "console_scripts": [
            "btd700=btd700.cli:main",
        ],
    },
    classifiers=[
        "Operating System :: POSIX :: Linux",
        "Programming Language :: Python :: 3",
        "Topic :: System :: Hardware :: Hardware Drivers",
    ],
)
