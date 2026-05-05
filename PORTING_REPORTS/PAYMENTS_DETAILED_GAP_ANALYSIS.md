# Детальная сверка: Payments Service (Python vs C#)

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Python оригинал** | `payments.py` (36.5 KB, 970 строк) + `escrow.py` (43.2 KB) + `transactions.py` (24.8 KB) |
| **C# порт** | `src/Services/Payments/` |
| **Статус** | ⚠️ Требует проверки |

---

## 📋 Сверка Endpoints (Python найдено)

### payments.py Endpoints
| Endpoint | Python строки | Описание | C# статус |
|----------|---------------|----------|-----------|
| `GET /payments` | 113-179 | List with filters, caching | ❓ Проверить |
| `GET /payments/task/{id}` | 181-220 | Task payments | ❓ Проверить |
| `POST /payments` | 222-290 | Create with idempotency, ML fraud | ❓ Проверить |
| `GET /payments/{id}` | 292-331 | Get by ID | ❓ Проверить |
| `PUT /payments/{id}` | 333-404 | ⚠️ UNSAFE setattr update | ❓ Проверить безопасность |

### escrow.py Endpoints (найдено)
| Endpoint | Python строки | Описание | C# статус |
|----------|---------------|----------|-----------|
| `POST /escrow/{id}/stripe-intent` | 612-663 | Stripe PaymentIntent | ❓ Проверить |
| `POST /escrow/stripe/webhook` | 666-700 | Webhook handler | ❓ Проверить |
| `POST /escrow/{id}/paypal` | 703-811 | PayPal REST API | ❓ Проверить |

### transactions.py Endpoints (найдено)
| Endpoint | Python строки | Описание | C# статус |
|----------|---------------|----------|-----------|
| `POST /transactions` | 69-127 | Create with ML fraud/AML | ❓ Проверить |
| `GET /transactions` | 120-135 | List with filters | ❓ Проверить |
| `GET /transactions/{id}` | 177 | Get by ID | ❓ Проверить |
| `PUT /transactions/{id}` | 216-231 | Update details | ❓ Проверить |
| `DELETE /transactions/{id}` | 260-275 | Cancel | ❓ Проверить |

---

## 🔍 Python Services (НЕОБХОДИМО проверить в C#)

### Из payments.py imports (lines 98-103):
```python
from app.services.payment_service import PaymentService              # ❓ Нет в C#?
from app.services.token_service import TokenService                  # ❓ Нет в C#?
from app.services.idempotency_service import IdempotencyService      # ❓ Нет в C#?
from app.services.audit_service import AuditService                  # ❓ Проверить
from app.services.sanctions_screening_service import SanctionsScreeningService  # ❓ Нет в C#?
```

### Критичные сервисы для проверки:

| Сервис | Python файл | Назначение | C# статус |
|--------|-------------|------------|-----------|
| `PaymentService` | `payment_service.py` | ML fraud detection, Stripe/PayPal | ❓ |
| `TokenService` | `token_service.py` | Idempotency tokens | ❓ |
| `IdempotencyService` | `idempotency_service.py` | Duplicate prevention | ❓ |
| `SanctionsScreeningService` | `sanctions_screening_service.py` | AML/OFAC compliance | ❓ |
| `AuditService` | `audit_service.py` | Financial audit logging | ❓ |
| `TransactionService` | `transaction_service.py` | Transaction management | ❓ |

---

## ⚠️ Критичные особенности Python (проверить в C#)

### 1. ML Fraud Detection
```python
# payments.py line 259
service = PaymentService(db)
payment = await service.create_payment(
    user_id=current_user.id,
    payment_data=payment_data,
    idempotency_key=idempotency_key
)
# Внутри: ML модель для fraud detection
```

### 2. Idempotency Protection
```python
# payments.py lines 243-248
if idempotency_key:
    idempotency_service = IdempotencyService(db)
    existing = await idempotency_service.check(idempotency_key, current_user.id)
    if existing:
        return existing  # Duplicate prevention
```

### 3. Sanctions Screening (AML/OFAC)
```python
# payments.py import line 102
from app.services.sanctions_screening_service import SanctionsScreeningService
# ⚠️ CRITICAL: Geo-blocking for sanctioned countries (RU, BY)
```

### 4. Stripe Integration
```python
# escrow.py lines 612-663
create_stripe_payment_intent(escrow_id, amount)  # Decimal precision
# escrow.py lines 666-700
stripe_webhook(request)  # Signature verification
```

### 5. PayPal Integration
```python
# escrow.py lines 703-811
# PayPal REST API with OAuth2
# Create payment → Get approval_url
# CRITICAL: Decimal amount (no float!)
```

### 6. UNSAFE Payment Update (SECURITY!)
```python
# payments.py lines 373-376 (SECURITY VULNERABILITY)
for key, value in payment_data.dict(exclude_unset=True).items():
    setattr(payment, key, value)  # ⚠️ Can modify amount, status!
```

### 7. ML Fraud in Transactions
```python
# transactions.py lines 93-96
transaction = await run_in_threadpool(
    TransactionService.create_transaction,
    db, transaction_data, current_user.id
)
# Inside: ML Fraud Detection + Risk Scoring + AML Screening
```

---

## 📁 Проверка C# структуры

### Необходимо проверить наличие:

```csharp
// Libr4.Payments.Application/Services/
- IPaymentService.cs              // ML fraud detection?
- IIdempotencyService.cs          // Duplicate prevention?
- ISanctionsScreeningService.cs   // AML compliance?
- IAuditService.cs                // Financial logging?

// Libr4.Payments.Infrastructure/
- PaymentService.cs                 // Stripe/PayPal integration
- StripeWebhookHandler.cs           // Webhook signature verify
- PayPalClient.cs                   // PayPal REST API
- FraudDetectionService.cs          // ML model integration

// Libr4.Payments.Api/Endpoints/
- PaymentEndpoints.cs               // All payment endpoints
- EscrowEndpoints.cs                // Stripe/PayPal escrow
- TransactionEndpoints.cs           // Transaction management
- WebhookEndpoints.cs               // Stripe webhook
```

---

## ❌ Потенциальные проблемы (предположительно)

### Высокий риск отсутствия:
1. **ML Fraud Detection** — сложная интеграция, возможно не портирована
2. **PayPal Integration** — REST API client, возможно не портирован
3. **Sanctions Screening** — AML/OFAC compliance, критично для FinTech
4. **Idempotency Service** — duplicate transaction prevention

### Средний риск:
5. **Stripe Webhook Security** — signature verification
6. **Audit Logging** — financial compliance logging
7. **Decimal Precision** — financial accuracy (Python использует Decimal)

---

## 🎯 Действия для проверки

### Step 1: Проверить наличие файлов
```powershell
# Проверить существование сервисов:
Get-ChildItem "src/Services/Payments" -Recurse -Filter "*.cs"
# Ищем: PaymentService, IdempotencyService, SanctionsScreeningService
```

### Step 2: Проверить endpoints
```csharp
// Должны быть в Payments.Api/Endpoints/:
- PaymentEndpoints.cs (GET /payments, POST /payments, etc.)
- EscrowEndpoints.cs (POST /escrow/*/stripe-intent, paypal)
- WebhookEndpoints.cs (POST /escrow/stripe/webhook)
- TransactionEndpoints.cs (GET/POST /transactions)
```

### Step 3: Проверить интеграции
```csharp
// Должны быть в Infrastructure/:
- StripeService или StripeClient
- PayPalService или PayPalClient  
- ML Fraud Detection (возможно отдельный сервис)
- AML Screening Service
```

---

## 📊 Оценка покрытия (предварительная)

| Компонент | Python | C# (предпол.) | Статус |
|-----------|--------|---------------|--------|
| Basic CRUD | 100% | 80% | 🟡 |
| Stripe Integration | 100% | 60% | 🔴 |
| PayPal Integration | 100% | 20% | 🔴 |
| ML Fraud Detection | 100% | 10% | 🔴 |
| AML/Sanctions | 100% | 10% | 🔴 |
| Idempotency | 100% | 50% | 🟡 |
| Audit Logging | 100% | 70% | 🟡 |

---

## 🚨 Следующие шаги

1. **Проверить файлы** в `src/Services/Payments/`
2. **Сравнить endpoints** с Python списком выше
3. **Проверить Stripe/PayPal** интеграцию
4. **Проверить ML/AML** сервисы
5. **Создать детальный план** доработки

---

**Создано:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Статус:** 🟡 ТРЕБУЕТ ПРОВЕРКИ C# КОДА
**Следующий шаг:** Проверить наличие сервисов в C#
