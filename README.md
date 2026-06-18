# 🚀 Local Multi-Agent PR Gatekeeper Pipeline

A lightweight, enterprise-grade backend prototype demonstrating deterministic **Agent-to-Agent (A2A)** communication, state boundaries, and strict structural type-safety using the **Microsoft Agent Framework 1.0 (GA)**.

The entire pipeline runs **100% free and locally** using local compute via Ollama, ensuring data privacy and zero cloud token dependencies.

---

## 🏗️ Architecture Design Patterns

Most basic AI implementations rely on a single, massive prompt wrapper that handles multiple conflicting tasks—leading to high hallucination rates and brittle outputs. This project implements a **Segregation of Concerns** pattern by breaking the workflow down into two specialized agent nodes governed by an orchestrator loop.



### 1. The Security Auditor Agent (`SecurityAuditor`)
* **Role:** Acts as an automated Static Application Security Testing (SAST) tool.
* **Focus:** Scans raw code input blocks looking for plain-text secrets, hardcoded configurations, or dangerous variable concatenations (SQL injections).
* **Output:** Constrained by the runtime layer to output a strict, strongly-typed JSON schema contract matching our application's `AuditResult` blueprint.

### 2. The Remediation Engineer Agent (`RefactoringEngineer`)
* **Role:** Acts as a specialized code refactoring entity.
* **Focus:** Triggered downstream *only* if the baseline security audit flags a failure. It consumes the original context along with the structured audit findings to emit a safe, parameterized implementation.

### 3. Deterministic Control Flow (State Orchestration)
Instead of relying on an LLM to choose its own path, the orchestrator utilizes explicit software runtime control code (`if (!auditResult.IsApproved)`) to dictate execution flow. This model-agnostic layout guarantees predictable execution patterns inside a larger microservice fabric.

---

## 🛠️ Tech Stack & Dependencies

* **Runtime:** .NET 8.0 / .NET 10.0 Console Architecture
* **Orchestration Framework:** `Microsoft.Agents.AI` (Agent Framework 1.0 GA)
* **Abstractions Layer:** `Microsoft.Extensions.AI`
* **Local Compute Engine:** Ollama (Inferencing `phi4` reasoning model)

---

## ⚙️ Local Machine Setup

### 1. Configure the Local LLM Gateway
Ensure you have [Ollama](https://ollama.com) installed and running locally. Open your terminal and pull down the model payload:

```bash
# Pull Microsoft's optimized open reasoning model
ollama pull phi4
