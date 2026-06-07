using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Stack-wide deterministic fixes for Java/Spring backend compile drift in backend/ monorepos.
/// </summary>
public static class JavaSpringCompileRemediation
{
    private static readonly Regex RepositoryInnerTypeRef = new(
        @"\b\w+\.\w+(Status|Type)\b",
        RegexOptions.Compiled);

    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors)
    {
        if (!JavaMonorepoPaths.IsJavaReactPlan(plan))
            return 0;

        var blob = string.Join('\n', errors.Select(e => $"{e.ErrorType} {e.Message} {e.FilePath}"));
        var allSources = string.Join('\n', files.Select(f => f.Content));
        var basePackage = JavaMonorepoPaths.InferBasePackage(files);

        var changed = 0;
        if (ReferencesSymbol(allSources, blob, "findByUserId"))
            changed += EnsureReferencedRepositoryMethods(files, "findByUserId", "Long userId");

        if (ReferencesSymbol(allSources, blob, "JwtTokenProvider")
            || ReferencesSymbol(allSources, blob, "AuthTokenResponse"))
            changed += EnsureJwtArtifacts(files, basePackage);

        if (ReferencesSymbol(allSources, blob, "getRoles"))
            changed += EnsureUserRolesMethod(files);

        changed += JavaBankingMonorepoRemediation.Apply(files, plan);
        changed += NormalizeJavaSourceDrift(files);
        changed += RepairMalformedJpaRepositories(files);
        changed += SimplifyBrokenJpaRepositories(files);
        changed += EnsureAuthSupportArtifacts(files, basePackage);
        changed += AlignAuthControllerSignature(files);

        return changed;
    }

    private static bool ReferencesSymbol(string sources, string blob, string symbol) =>
        sources.Contains(symbol, StringComparison.Ordinal)
        || blob.Contains(symbol, StringComparison.OrdinalIgnoreCase);

    private static int NormalizeJavaSourceDrift(IList<GeneratedFile> files)
    {
        var changed = 0;
        foreach (var file in JavaMonorepoPaths.BackendJavaFiles(files).ToList())
        {
            var idx = files.IndexOf(file);
            var content = file.Content ?? string.Empty;
            var updated = content;
            updated = updated.Replace(".fromAccountNumber()", ".sourceAccountNumber()", StringComparison.Ordinal);
            updated = updated.Replace(".toAccountNumber()", ".destinationAccountNumber()", StringComparison.Ordinal);
            updated = updated.Replace("setFromAccount", "setSourceAccount", StringComparison.Ordinal);
            updated = updated.Replace("setToAccount", "setDestinationAccount", StringComparison.Ordinal);
            updated = updated.Replace("setType(", "setTransactionType(", StringComparison.Ordinal);
            updated = Regex.Replace(updated, @"^\s*\w+\.setTimestamp\([^;]*\);\s*\r?\n", string.Empty, RegexOptions.Multiline);
            updated = Regex.Replace(updated, @"^\s*\w+\.setTransactionDate\([^;]*\);\s*\r?\n", string.Empty, RegexOptions.Multiline);

            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[idx] = new GeneratedFile(file.RelativePath, file.Language, updated);
            changed++;
        }

        return changed;
    }

    private static int RepairMalformedJpaRepositories(IList<GeneratedFile> files)
    {
        var changed = 0;
        foreach (var repo in JavaMonorepoPaths.BackendRepositories(files).ToList())
        {
            var content = repo.Content ?? string.Empty;
            if (!HasOrphanRepositoryMethods(content))
                continue;

            var entity = JavaMonorepoPaths.ExtractEntityNameFromRepository(content);
            var package = JavaMonorepoPaths.ExtractPackage(content);
            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(package))
                continue;

            var minimal = BuildMinimalRepository(package, entity, content);
            if (string.Equals(minimal, content, StringComparison.Ordinal))
                continue;

            var idx = files.IndexOf(repo);
            files[idx] = new GeneratedFile(repo.RelativePath, repo.Language, minimal);
            changed++;
        }

        return changed;
    }

    private static bool HasOrphanRepositoryMethods(string content)
    {
        var ifaceIdx = content.IndexOf("public interface", StringComparison.Ordinal);
        if (ifaceIdx < 0)
            return false;

        var prelude = content[..ifaceIdx];
        return Regex.IsMatch(
            prelude,
            @"(?:^|\n)\s*(?:java\.util\.)?\S+\s+\w+\([^)]*\);\s*(?:\r?\n)",
            RegexOptions.Multiline);
    }

    private static int SimplifyBrokenJpaRepositories(IList<GeneratedFile> files)
    {
        var changed = 0;
        foreach (var repo in JavaMonorepoPaths.BackendRepositories(files).ToList())
        {
            var idx = files.IndexOf(repo);
            var content = repo.Content ?? string.Empty;
            var broken = HasOrphanRepositoryMethods(content)
                         || content.Contains("sourceAccountId", StringComparison.Ordinal)
                         || content.Contains("destinationAccountId", StringComparison.Ordinal)
                         || content.Contains("transactionReference", StringComparison.Ordinal)
                         || RepositoryInnerTypeRef.IsMatch(content);
            if (!broken)
                continue;

            var entity = JavaMonorepoPaths.ExtractEntityNameFromRepository(content);
            var package = JavaMonorepoPaths.ExtractPackage(content);
            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(package))
                continue;

            var minimal = BuildMinimalRepository(package, entity, content);
            if (string.Equals(minimal, content, StringComparison.Ordinal))
                continue;

            files[idx] = new GeneratedFile(repo.RelativePath, repo.Language, minimal);
            changed++;
        }

        return changed;
    }

    private static string BuildMinimalRepository(string package, string entity, string existing)
    {
        var modelNs = package.EndsWith(".repository", StringComparison.Ordinal)
            ? package[..^".repository".Length] + ".model"
            : package + ".model";

        var ifaceMatch = Regex.Match(existing, @"public\s+interface\s+(\w+Repository)\b");
        var ifaceName = ifaceMatch.Success ? ifaceMatch.Groups[1].Value : entity + "Repository";

        var extras = new List<string>();
        if (existing.Contains("findByAccountNumber", StringComparison.Ordinal))
            extras.Add($"    java.util.Optional<{entity}> findByAccountNumber(String accountNumber);");
        if (existing.Contains("findByUserId", StringComparison.Ordinal))
            extras.Add($"    java.util.List<{entity}> findByUserId(Long userId);");
        if (existing.Contains("existsByAccountNumber", StringComparison.Ordinal))
            extras.Add("    boolean existsByAccountNumber(String accountNumber);");

        var methods = extras.Count > 0 ? "\n" + string.Join("\n", extras) + "\n" : string.Empty;
        return $$"""
            package {{package}};

            import {{modelNs}}.{{entity}};
            import org.springframework.data.jpa.repository.JpaRepository;
            import org.springframework.stereotype.Repository;

            @Repository
            public interface {{ifaceName}} extends JpaRepository<{{entity}}, Long> {{{methods}}}
            """;
    }

    private static int EnsureReferencedRepositoryMethods(
        IList<GeneratedFile> files,
        string methodName,
        string signatureSuffix)
    {
        var changed = 0;
        foreach (var repo in JavaMonorepoPaths.BackendRepositories(files).ToList())
        {
            if ((repo.Content ?? string.Empty).Contains(methodName + "(", StringComparison.Ordinal))
                continue;

            var entity = JavaMonorepoPaths.ExtractEntityNameFromRepository(repo.Content ?? string.Empty);
            if (string.IsNullOrWhiteSpace(entity))
                continue;

            var returnType = methodName.StartsWith("find", StringComparison.Ordinal) && methodName.Contains("ById", StringComparison.Ordinal)
                ? $"java.util.Optional<{entity}>"
                : methodName.StartsWith("find", StringComparison.Ordinal)
                    ? $"java.util.List<{entity}>"
                    : "void";

            var method = $"\n    {returnType} {methodName}({signatureSuffix});\n";
            var updated = InsertBeforeInterfaceClosingBrace(repo.Content ?? string.Empty, method);
            if (string.Equals(updated, repo.Content, StringComparison.Ordinal))
                continue;

            var idx = files.IndexOf(repo);
            files[idx] = new GeneratedFile(repo.RelativePath, repo.Language, updated);
            changed++;
        }

        return changed;
    }

    private static int EnsureJwtArtifacts(IList<GeneratedFile> files, string basePackage)
    {
        var changed = 0;
        var dtoPath = JavaMonorepoPaths.BackendMainJava(basePackage, "dto/AuthTokenResponse.java");
        var jwtPath = JavaMonorepoPaths.BackendMainJava(basePackage, "security/JwtTokenProvider.java");

        if (!files.Any(f => f.RelativePath.Equals(dtoPath, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(new GeneratedFile(dtoPath, "java", BuildAuthTokenResponse(basePackage)));
            changed++;
        }

        if (!files.Any(f => f.RelativePath.Equals(jwtPath, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(new GeneratedFile(jwtPath, "java", BuildJwtTokenProvider(basePackage)));
            changed++;
        }

        return changed;
    }

    private static int EnsureUserRolesMethod(IList<GeneratedFile> files)
    {
        var user = JavaMonorepoPaths.BackendJavaFiles(files)
            .FirstOrDefault(f => f.RelativePath.Contains("/model/", StringComparison.OrdinalIgnoreCase)
                                 && f.RelativePath.EndsWith("User.java", StringComparison.OrdinalIgnoreCase));
        if (user is null)
            return 0;

        var content = user.Content ?? string.Empty;
        if (content.Contains("getRoles()", StringComparison.Ordinal))
            return 0;

        const string method = """

            public java.util.Set<String> getRoles() {
                return java.util.Set.of("ROLE_USER");
            }
            """;

        var updated = InsertBeforeLastBrace(content, method);
        if (string.Equals(updated, content, StringComparison.Ordinal))
            return 0;

        var idx = files.IndexOf(user);
        files[idx] = new GeneratedFile(user.RelativePath, user.Language, updated);
        return 1;
    }

    private static int EnsureAuthSupportArtifacts(IList<GeneratedFile> files, string basePackage)
    {
        if (!files.Any(f => f.RelativePath.EndsWith("AuthService.java", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var changed = 0;
        var userRepoPath = JavaMonorepoPaths.BackendMainJava(basePackage, "repository/UserRepository.java");
        if (!files.Any(f => f.RelativePath.Equals(userRepoPath, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(new GeneratedFile(userRepoPath, "java", BuildUserRepository(basePackage)));
            changed++;
        }

        var securityConfig = JavaMonorepoPaths.FindByFileName(files, "SecurityConfig.java");
        var jwtFilterPath = JavaMonorepoPaths.BackendMainJava(basePackage, "security/JwtAuthenticationFilter.java");

        if (securityConfig?.Content?.Contains("JwtAuthenticationFilter", StringComparison.Ordinal) == true
            && !files.Any(f => f.RelativePath.Equals(jwtFilterPath, StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(new GeneratedFile(jwtFilterPath, "java", BuildJwtAuthenticationFilter(basePackage)));
            changed++;
        }

        if (securityConfig is not null)
        {
            var idx = files.IndexOf(securityConfig);
            var content = securityConfig.Content ?? string.Empty;
            var updated = content;
            var filterImport = $"import {basePackage}.security.JwtAuthenticationFilter;";
            if (content.Contains("JwtAuthenticationFilter", StringComparison.Ordinal)
                && !content.Contains(filterImport, StringComparison.Ordinal))
            {
                updated = updated.Replace(
                    "import org.springframework.context.annotation.Bean;",
                    filterImport + "\nimport org.springframework.context.annotation.Bean;",
                    StringComparison.Ordinal);
            }

            if (!content.Contains("AuthenticationManager", StringComparison.Ordinal))
                updated = BuildSecurityConfigWithAuthManager(basePackage);

            if (!string.Equals(updated, content, StringComparison.Ordinal))
            {
                files[idx] = new GeneratedFile(securityConfig.RelativePath, securityConfig.Language, updated);
                changed++;
            }
        }

        return changed;
    }

    private static int AlignAuthControllerSignature(IList<GeneratedFile> files)
    {
        var authController = JavaMonorepoPaths.FindByFileName(files, "AuthController.java");
        if (authController is null)
            return 0;

        var content = authController.Content ?? string.Empty;
        if (!content.Contains("authService.authenticate(request.username()", StringComparison.Ordinal)
            && !content.Contains("authService.authenticate(request.getUsername()", StringComparison.Ordinal))
            return 0;

        var updated = Regex.Replace(
            content,
            @"authService\.authenticate\s*\(\s*request\.username\s*\(\s*\)\s*,\s*request\.password\s*\(\s*\)\s*\)",
            "authService.authenticate(request)",
            RegexOptions.Singleline);

        updated = Regex.Replace(
            updated,
            @"String\s+token\s*=\s*authService\.authenticate\s*\(\s*request\s*\)\s*;",
            "AuthTokenResponse response = authService.authenticate(request);",
            RegexOptions.Singleline);

        if (!updated.Contains("AuthTokenResponse", StringComparison.Ordinal)
            && updated.Contains("authService.authenticate(request)", StringComparison.Ordinal))
        {
            var package = JavaMonorepoPaths.ExtractPackage(updated) ?? "com.generated.app";
            updated = updated.Replace(
                $"package {package};",
                $"package {package};\n\nimport {package}.dto.AuthTokenResponse;",
                StringComparison.Ordinal);
        }

        updated = updated.Replace(
            "return ResponseEntity.ok(Map.of(\"token\", token));",
            "return ResponseEntity.ok(response);",
            StringComparison.Ordinal);

        if (string.Equals(updated, content, StringComparison.Ordinal))
            return 0;

        var idx = files.IndexOf(authController);
        files[idx] = new GeneratedFile(authController.RelativePath, authController.Language, updated);
        return 1;
    }

    private static string InsertBeforeLastBrace(string content, string insertion)
    {
        var idx = content.LastIndexOf('}');
        return idx < 0 ? content : content.Insert(idx, insertion);
    }

    private static string InsertBeforeInterfaceClosingBrace(string content, string insertion)
    {
        var match = Regex.Match(content, @"\}\s*$", RegexOptions.Singleline);
        return match.Success
            ? content.Insert(match.Index, insertion)
            : InsertBeforeLastBrace(content, insertion);
    }

    private static string BuildAuthTokenResponse(string basePackage) => $$"""
        package {{basePackage}}.dto;

        import java.util.Set;

        public record AuthTokenResponse(
                String token,
                String refreshToken,
                String tokenType,
                long expiresIn,
                Long userId,
                String username,
                Set<String> roles
        ) {
        }
        """;

    private static string BuildUserRepository(string basePackage) => $$"""
        package {{basePackage}}.repository;

        import {{basePackage}}.model.User;
        import org.springframework.data.jpa.repository.JpaRepository;
        import org.springframework.stereotype.Repository;

        import java.util.Optional;

        @Repository
        public interface UserRepository extends JpaRepository<User, Long> {
            Optional<User> findByUsername(String username);
        }
        """;

    private static string BuildJwtAuthenticationFilter(string basePackage) => $$"""
        package {{basePackage}}.security;

        import jakarta.servlet.FilterChain;
        import jakarta.servlet.ServletException;
        import jakarta.servlet.http.HttpServletRequest;
        import jakarta.servlet.http.HttpServletResponse;
        import org.springframework.security.core.context.SecurityContextHolder;
        import org.springframework.stereotype.Component;
        import org.springframework.web.filter.OncePerRequestFilter;

        import java.io.IOException;

        @Component
        public class JwtAuthenticationFilter extends OncePerRequestFilter {

            private final JwtTokenProvider jwtTokenProvider;

            public JwtAuthenticationFilter(JwtTokenProvider jwtTokenProvider) {
                this.jwtTokenProvider = jwtTokenProvider;
            }

            @Override
            protected void doFilterInternal(
                    HttpServletRequest request,
                    HttpServletResponse response,
                    FilterChain filterChain) throws ServletException, IOException {
                String header = request.getHeader("Authorization");
                if (header != null && header.startsWith("Bearer ")) {
                    String token = header.substring(7);
                    if (jwtTokenProvider.validateToken(token)) {
                        SecurityContextHolder.getContext().setAuthentication(
                                jwtTokenProvider.getAuthentication(token));
                    }
                }
                filterChain.doFilter(request, response);
            }
        }
        """;

    private static string BuildSecurityConfigWithAuthManager(string basePackage) => $$"""
        package {{basePackage}}.config;

        import {{basePackage}}.security.JwtAuthenticationFilter;
        import org.springframework.context.annotation.Bean;
        import org.springframework.context.annotation.Configuration;
        import org.springframework.security.authentication.AuthenticationManager;
        import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
        import org.springframework.security.config.annotation.web.builders.HttpSecurity;
        import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
        import org.springframework.security.config.http.SessionCreationPolicy;
        import org.springframework.security.core.userdetails.User;
        import org.springframework.security.core.userdetails.UserDetailsService;
        import org.springframework.security.provisioning.InMemoryUserDetailsManager;
        import org.springframework.security.web.SecurityFilterChain;
        import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

        @Configuration
        @EnableWebSecurity
        public class SecurityConfig {

            private final JwtAuthenticationFilter jwtAuthenticationFilter;

            public SecurityConfig(JwtAuthenticationFilter jwtAuthenticationFilter) {
                this.jwtAuthenticationFilter = jwtAuthenticationFilter;
            }

            @Bean
            public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
                http
                    .csrf(csrf -> csrf.disable())
                    .sessionManagement(session -> session.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                    .authorizeHttpRequests(auth -> auth
                        .requestMatchers("/actuator/health", "/actuator/readiness", "/api/auth/**").permitAll()
                        .anyRequest().authenticated()
                    )
                    .httpBasic(httpBasic -> httpBasic.disable())
                    .formLogin(formLogin -> formLogin.disable())
                    .addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);

                return http.build();
            }

            @Bean
            public AuthenticationManager authenticationManager(AuthenticationConfiguration configuration) throws Exception {
                return configuration.getAuthenticationManager();
            }

            @Bean
            public UserDetailsService userDetailsService() {
                return new InMemoryUserDetailsManager(
                        User.withUsername("demo")
                                .password("{noop}demo")
                                .roles("USER")
                                .build());
            }
        }
        """;

    private static string BuildJwtTokenProvider(string basePackage) => $$"""
        package {{basePackage}}.security;

        import {{basePackage}}.model.User;
        import io.jsonwebtoken.Claims;
        import io.jsonwebtoken.Jwts;
        import io.jsonwebtoken.security.Keys;
        import org.springframework.beans.factory.annotation.Value;
        import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
        import org.springframework.security.core.Authentication;
        import org.springframework.security.core.authority.SimpleGrantedAuthority;
        import org.springframework.stereotype.Component;

        import javax.crypto.SecretKey;
        import java.nio.charset.StandardCharsets;
        import java.time.Duration;
        import java.util.Date;
        import java.util.List;
        import java.util.Set;
        import java.util.concurrent.ConcurrentHashMap;

        @Component
        public class JwtTokenProvider {

            private final SecretKey secretKey;
            private final Duration expiration;
            private final Set<String> invalidated = ConcurrentHashMap.newKeySet();

            public JwtTokenProvider(@Value("${jwt.secret:change-me-in-production-change-me}") String secret) {
                var padded = secret.length() >= 32 ? secret : (secret + "0123456789012345678901234567890").substring(0, 32);
                this.secretKey = Keys.hmacShaKeyFor(padded.getBytes(StandardCharsets.UTF_8));
                this.expiration = Duration.ofHours(24);
            }

            public String generateToken(Authentication authentication) {
                var now = new Date();
                var expiry = new Date(now.getTime() + expiration.toMillis());
                return Jwts.builder()
                        .subject(authentication.getName())
                        .issuedAt(now)
                        .expiration(expiry)
                        .signWith(secretKey)
                        .compact();
            }

            public String generateRefreshToken(User user) {
                var now = new Date();
                var expiry = new Date(now.getTime() + expiration.multipliedBy(7).toMillis());
                return Jwts.builder()
                        .subject(user.getUsername())
                        .claim("type", "refresh")
                        .issuedAt(now)
                        .expiration(expiry)
                        .signWith(secretKey)
                        .compact();
            }

            public boolean validateToken(String token) {
                if (token == null || invalidated.contains(token))
                    return false;
                try {
                    Jwts.parser().verifyWith(secretKey).build().parseSignedClaims(token);
                    return true;
                } catch (Exception ex) {
                    return false;
                }
            }

            public String getUsernameFromToken(String token) {
                Claims claims = Jwts.parser().verifyWith(secretKey).build()
                        .parseSignedClaims(token).getPayload();
                return claims.getSubject();
            }

            public Authentication getAuthentication(String token) {
                var username = getUsernameFromToken(token);
                var authorities = List.of(new SimpleGrantedAuthority("ROLE_USER"));
                return new UsernamePasswordAuthenticationToken(username, null, authorities);
            }

            public Duration getExpirationDuration() {
                return expiration;
            }

            public void invalidateToken(String token) {
                if (token != null)
                    invalidated.add(token);
            }
        }
        """;
}
