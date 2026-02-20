# PROTOCOLO 001 — Intensive Production Readiness Audit

## Overview

This protocol launches **8 parallel audit agents** (all using Opus) that perform a comprehensive, intensive audit of the entire My-Negocio codebase. The goal is to identify every possible issue before pushing to production.

## How to Run

Simply tell Claude:

```
run protocolo 001
```

Claude will automatically launch all 8 agents in parallel. Each agent audits a specific domain and returns a detailed report with findings categorized by severity (CRITICAL / HIGH / MEDIUM / LOW).

## Audit Agents

### Agent 1: SECURITY AUDIT
**Scope:** Authentication, authorization, injection, XSS, CSRF, data exposure
- Cookie configuration (HttpOnly, Secure, SameSite)
- BCrypt implementation and lazy migration safety
- SQL injection via raw queries or string interpolation in EF
- XSS in Razor views (unescaped `@Html.Raw`, JS injection points)
- CSRF token validation on all POST/PUT/DELETE endpoints
- Multi-tenant isolation (every query filtered by NegocioId)
- Admin impersonation security
- Rate limiting configuration
- Security headers (CSP, HSTS, X-Frame-Options, X-Content-Type-Options)
- Secrets in source code (connection strings, API keys, passwords)
- Password validation rules
- Session/cookie expiration policies

### Agent 2: DATABASE & DATA INTEGRITY
**Scope:** EF Core, migrations, queries, data consistency
- Missing indexes on frequently queried columns
- N+1 query problems (lazy loading traps)
- Missing foreign key constraints
- Orphaned records risk (cascade delete configuration)
- Migration safety (data loss risk in pending migrations)
- Transaction boundaries (operations that should be atomic)
- Concurrency conflicts (no optimistic concurrency tokens)
- DbContext lifetime issues (scoped vs singleton)
- Raw SQL injection points
- Nullable reference handling on required fields
- Decimal precision for financial amounts
- DateTime timezone consistency (UTC vs local)

### Agent 3: API & CONTROLLER AUDIT
**Scope:** All 15 controllers, endpoints, request validation
- Missing `[Authorize]` attributes
- Missing input validation on POST endpoints
- Missing null checks on entity lookups (404 vs 500)
- Inconsistent error responses (JSON vs redirect vs view)
- Missing model validation (`ModelState.IsValid` checks)
- File upload security (Excel import)
- AJAX endpoints returning sensitive data
- HTTP method misuse (GET with side effects)
- Route conflicts or ambiguity
- Missing pagination on list endpoints
- Exception handling gaps (unhandled exceptions leaking to client)
- Proper HTTP status codes

### Agent 4: BACKGROUND SERVICES AUDIT
**Scope:** 4 background services, email system, WhatsApp integration
- Service crash recovery (what happens if a service throws?)
- Memory leaks in long-running services
- DbContext scope management (IServiceScopeFactory usage)
- Email delivery failure handling
- WhatsApp API error handling and retry logic
- Timezone correctness (Ecuador UTC-5)
- Race conditions in concurrent execution
- Graceful shutdown behavior (CancellationToken)
- Duplicate message prevention
- Service health monitoring
- Configuration validation on startup
- Dead letter / failed message tracking

### Agent 5: FRONTEND & UI AUDIT
**Scope:** All Razor views, JavaScript, CSS, UX
- JavaScript errors (undefined variables, null references)
- Missing loading states on AJAX calls
- Missing error handling on fetch/$.ajax failures
- Form validation (client-side + server-side alignment)
- Mobile responsiveness (modal-fullscreen-sm-down usage)
- Broken links or dead routes
- Theme system consistency (all 4 themes render correctly)
- DataTables configuration (pagination, search, export)
- SweetAlert2 / Toastr proper usage
- Accessibility (alt texts, aria labels, color contrast)
- Asset loading (missing files, 404s)
- XSS via JavaScript DOM manipulation

### Agent 6: BUSINESS LOGIC AUDIT
**Scope:** Service layer, commission system, membership logic, appointment system
- Commission calculation correctness (flat $5, one-time per business)
- Membership expiration logic and edge cases
- Appointment booking conflicts (double booking prevention)
- Inventory stock going negative
- Financial calculations (rounding, precision)
- Notification generation correctness (3-day / 1-day alerts)
- Daily report accuracy
- Payment recording integrity
- Subscription/trial logic correctness
- Edge cases: midnight boundaries, DST changes, leap years
- Business type separation (membresias vs artesanal logic bleed)

### Agent 7: PERFORMANCE & SCALABILITY
**Scope:** Query optimization, caching, resource usage
- Unbounded queries (SELECT * without pagination)
- Missing `.AsNoTracking()` on read-only queries
- Large data transfers in AJAX responses
- Memory allocation patterns in hot paths
- Static file caching headers
- Response compression
- Connection pool exhaustion risk
- Synchronous I/O on async paths
- Large file handling (Excel import/export)
- Background service CPU/memory usage patterns
- Database connection management

### Agent 8: PRODUCTION DEPLOYMENT AUDIT
**Scope:** Configuration, environment, health checks, error handling
- `appsettings.json` vs `appsettings.Production.json` separation
- Health check completeness (`/health` endpoint)
- Auto-migration safety on startup
- Logging configuration (structured logging, log levels)
- Error pages (custom 404, 500 pages)
- HTTPS enforcement
- CORS configuration
- Environment-specific settings (dev vs prod)
- Startup failure modes (what if DB is down?)
- Graceful degradation (what if WhatsApp API is down?)
- Docker/reverse proxy configuration
- Dependency versions and known vulnerabilities

## Output Format

Each agent produces a report with:

```
## [AGENT NAME] — Audit Report

### CRITICAL (Must fix before production)
1. [Finding] — [File:Line] — [Description + Fix]

### HIGH (Should fix before production)
1. [Finding] — [File:Line] — [Description + Fix]

### MEDIUM (Fix soon after launch)
1. [Finding] — [File:Line] — [Description + Fix]

### LOW (Nice to have)
1. [Finding] — [File:Line] — [Description + Fix]

### PASSED CHECKS
- [List of things that are correctly implemented]
```

## After the Audit

After all 8 agents complete, Claude will:
1. Compile a **consolidated report** with all findings
2. Count issues by severity: CRITICAL / HIGH / MEDIUM / LOW
3. Create a **prioritized fix list** ordered by impact
4. Optionally launch fix agents to resolve CRITICAL and HIGH issues

## Requirements
- All agents use **Opus** model for maximum accuracy
- Each agent reads ALL relevant files (not samples)
- Each agent provides specific file:line references
- Each agent suggests concrete fixes, not vague recommendations
