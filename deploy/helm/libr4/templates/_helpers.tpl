{{/*
Expand the name of the chart.
*/}}
{{- define "libr4.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "libr4.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "libr4.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "libr4.labels" -}}
helm.sh/chart: {{ include "libr4.chart" . }}
{{ include "libr4.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "libr4.selectorLabels" -}}
app.kubernetes.io/name: {{ include "libr4.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Service name helper
*/}}
{{- define "libr4.serviceName" -}}
{{ .service }}-service
{{- end }}

{{/*
Connection strings
*/}}
{{- define "libr4.postgresConnectionString" -}}
Host={{ .Values.global.postgresql.host }};Port={{ .Values.global.postgresql.port }};Database={{ .Values.global.postgresql.database }};Username={{ .Values.global.postgresql.username }};Password={{ .Values.global.postgresql.password }}
{{- end }}

{{- define "libr4.redisConnectionString" -}}
{{ .Values.global.redis.host }}:{{ .Values.global.redis.port }}
{{- end }}
