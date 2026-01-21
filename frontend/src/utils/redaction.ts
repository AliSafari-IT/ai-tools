export interface RedactionOptions {
    redactEmails?: boolean
    redactJwt?: boolean
    redactApiKeys?: boolean
    redactPasswords?: boolean
    redactConnectionStrings?: boolean
}

export function redactContent(text: string, options: RedactionOptions = {}): string {
    if (!text) return text

    const {
        redactEmails = true,
        redactJwt = true,
        redactApiKeys = true,
        redactPasswords = true,
        redactConnectionStrings = true
    } = options

    let redacted = text

    if (redactJwt) {
        redacted = redacted.replace(
            /Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+/g,
            'Bearer [REDACTED_JWT]'
        )
        redacted = redacted.replace(
            /\b[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\b/g,
            (match) => {
                const parts = match.split('.')
                if (parts.length === 3 && parts.every(p => p.length > 10)) {
                    return '[REDACTED_JWT]'
                }
                return match
            }
        )
    }

    if (redactApiKeys) {
        redacted = redacted.replace(/sk-[A-Za-z0-9]{20,}/g, '[REDACTED_OPENAI_KEY]')
        redacted = redacted.replace(/sk-proj-[A-Za-z0-9_-]{20,}/g, '[REDACTED_OPENAI_KEY]')
        redacted = redacted.replace(/(api[_-]?key|apikey)["']?\s*[:=]\s*["']?([^"'\s,}]+)/gi, '$1=[REDACTED_API_KEY]')
    }

    if (redactPasswords) {
        redacted = redacted.replace(/(password|pwd|secret|token)["']?\s*[:=]\s*["']?([^"'\s,};]+)/gi, '$1=[REDACTED]')
    }

    if (redactConnectionStrings) {
        redacted = redacted.replace(/(Server|Data Source|Initial Catalog|User ID|Password|Uid|Pwd)\s*=\s*[^;]+/gi, '$1=[REDACTED]')
    }

    if (redactEmails) {
        redacted = redacted.replace(/\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/g, '[REDACTED_EMAIL]')
    }

    return redacted
}

export function redactForClipboard(text: string): string {
    return redactContent(text, {
        redactEmails: true,
        redactJwt: true,
        redactApiKeys: true,
        redactPasswords: true,
        redactConnectionStrings: true
    })
}

export function redactForExport(text: string, includeEmails: boolean = false): string {
    return redactContent(text, {
        redactEmails: !includeEmails,
        redactJwt: true,
        redactApiKeys: true,
        redactPasswords: true,
        redactConnectionStrings: true
    })
}
