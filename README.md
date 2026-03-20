# Vuforia.NET

This repository contains low-level bindings for [Vuforia Engine](https://developer.vuforia.com/) used in [Evergine](https://evergine.com/).
This binding is generated from the Vuforia Engine SDK release v11.4.4.

[![CI](https://github.com/EvergineTeam/Vuforia.NET/actions/workflows/CI.yml/badge.svg)](https://github.com/EvergineTeam/Vuforia.NET/actions/workflows/CI.yml)
[![CD](https://github.com/EvergineTeam/Vuforia.NET/actions/workflows/CD.yml/badge.svg)](https://github.com/EvergineTeam/Vuforia.NET/actions/workflows/CD.yml)
[![Nuget](https://img.shields.io/nuget/v/Evergine.Bindings.Vuforia?logo=nuget)](https://www.nuget.org/packages/Evergine.Bindings.Vuforia)

## Purpose

Vuforia Engine is a comprehensive AR development platform that enables developers to build augmented reality experiences for mobile, headsets, and mixed reality devices. These .NET bindings provide direct P/Invoke access to the native Vuforia Engine C API, enabling integration with Evergine and other .NET applications.

See the [Vuforia Engine documentation](https://developer.vuforia.com/library/) for more details on the native library.

## Features

- **Image Target Tracking** — Recognize and track 2D images in the real world
- **Model Target Tracking** — Recognize and track 3D objects
- **Area Target Tracking** — Track large-scale environments and spaces
- **Device Pose Tracking** — Track device position and orientation in 6DoF
- **VuMark Recognition** — Recognize custom-designed markers
- **Barcode Scanning** — Detect and decode 1D and 2D barcodes
- **Cloud Image Targets** — Cloud-based image recognition
- **Mesh Observation** — Real-time 3D mesh generation
- **Session Recording** — Record and replay AR sessions

## Supported Platforms

- [x] Windows x64, ARM64
- [x] iOS ARM64
- [x] Android ARM64
