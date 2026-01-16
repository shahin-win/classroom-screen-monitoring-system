\# Classroom Screen Monitoring System (.NET)



\## Overview

A .NET-based real-time screen monitoring system designed for classroom and controlled lab environments. The system allows centralized monitoring of multiple client machines through a Server–Agent–Viewer architecture using SignalR.



\## Architecture

Agent → Server → Viewer



\## Components

\- Server: Central SignalR hub responsible for client registration and message routing

\- Agent: Installed on client machines to capture and transmit screen data

\- Viewer: Monitoring application for instructors or administrators



\## Technologies Used

\- C# / .NET

\- SignalR

\- Windows Forms

\- Networking \& Multithreading



\## How to Run

1\. Start the Server application

2\. Launch the Agent on client machines

3\. Open Viewer and connect to the Server



\## Disclaimer

This project is intended for educational and controlled environments only.



